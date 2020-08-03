using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EQLogParser;


namespace LogSync
{
    public partial class MainForm : Form
    {
        private readonly IConfigAdapter config;
        //private readonly SynchronizationContext syncContext;
        private CancellationTokenSource cancellationSource;
        private ConcurrentQueue<LogEvent> eventQueue;
        private SpellParser spells;
        private FightTracker fightTracker;
        private List<FightInfo> fightList;
        private List<FightInfo> fightSearchList;
        private Dictionary<string, string> fightStatus;
        private LootTracker lootTracker;
        private CharTracker charTracker;
        private List<LootInfo> lootList;
        private Uploader uploader;
        private int ignoredCount;

        public MainForm()
        {
            InitializeComponent();
            SetDoubleBuffered(lvFights, true);
            config = new RegConfigAdapter();
            eventQueue = new ConcurrentQueue<LogEvent>();
            spells = new SpellParser();
            fightTracker = new FightTracker(spells);
            fightTracker.OnFightFinished += LogFight;
            fightList = new List<FightInfo>();
            fightStatus = new Dictionary<string, string>();
            lootTracker = new LootTracker();
            lootTracker.OnLoot += LogLoot;
            lootList = new List<LootInfo>();
            charTracker = new CharTracker();
            uploader = new Uploader(LogInfo);
            lvFights.LabelEdit = false; // until i figure something more user friendly out
        }

        private async void MainForm_Shown(object sender, EventArgs e)
        {
            //LogInfo(Application.ExecutablePath);
            Text = Application.ProductName;

            // refresh the screen before we run tasks that may make it look frozen
            Refresh();

            // open the last log file as long as it wasn't an archived gzip file
            var path = config.Read("filename");
            if (File.Exists(path) && !path.EndsWith(".gz"))
            {
                textLogPath.Text = path;
                WatchFile(path);
            }

            _ = await uploader.Hello(config);
        }

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // the background task can finish after the form is disposed -- any attempts to update this form will then produce an exception
            // ideally we should be keeping an counter of background tasks and waiting until it returns to 0
            fightTracker.OnFightFinished -= LogFight;
            lootTracker.OnLoot -= LogLoot;
            if (cancellationSource != null)
            {
                cancellationSource.Cancel();
                await Task.Delay(500);
                //while (ActiveTasks > 0)
                //    await Task.Delay(100);
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                textSearch.Focus();
            }

            if (e.Control && e.KeyCode == Keys.A)
            {
                lvFights.BeginUpdate();
                for (var i = 0; i < lvFights.VirtualListSize; i++)
                    lvFights.SelectedIndices.Add(i);
                lvFights.EndUpdate();
            }
        }

        private async void btnUpload_Click(object sender, EventArgs e)
        {
            if (!uploader.IsReady)
            {
                // to avoid reentrancy issues we should probably abort after creating a continuation
                _ = await uploader.Hello(config);
                return;
            }

            if (lootList.Count > 0)
            {
                var posted = lootList;
                lootList = new List<LootInfo>();
                _ = uploader.UploadLoot(posted);
            }


            var selected = GetSelectedListViewFights();
            if (selected.Count == 0)
            {
                LogInfo("Nothing to upload. Select some fights from the fight list.");
                return;
            }

            foreach (var f in selected)
            {
                fightStatus[f.ID] = "Queued";
            }
            lvFights.Invalidate();

            foreach (var f in selected)
            {
                UploadFight(f);
                // throttle uploads if sending a large batch
                if (selected.Count > 10)
                    await Task.Delay(500);
            }
        }

        private void btnCombine_Click(object sender, EventArgs e)
        {
            // MobCount == 0 so that combined fights don't get combined into other fights
            var selected = GetSelectedListViewFights()
                .Where(x => x.MobCount == 0)
                .OrderBy(x => x.StartedOn)
                .ToList();

            var total = new MergedFightInfo(selected);
            total.Finish();
            if (total.MobCount > 1)
                LogFight(total);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            uploader.LaunchBrowser();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            var result = openLogDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                textLogPath.Text = openLogDialog.FileName;
                WatchFile(openLogDialog.FileName);
            }
        }

        private void textSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                textSearch.Text = "";
                lvFights.SelectedIndices.Clear();
            }
        }

        private void textSearch_TextChanged(object sender, EventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(textSearch.Text))
            {
                fightSearchList = fightList
                    .Where(x => x.Name.Contains(textSearch.Text, StringComparison.OrdinalIgnoreCase) || x.Zone.Contains(textSearch.Text, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                lvFights.VirtualListSize = fightSearchList.Count;
            }
            else
            {
                fightSearchList = null;
                lvFights.VirtualListSize = fightList.Count;
            }
            lvFights.SelectedIndices.Clear();
        }

        private void lvFights_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            var f = GetListViewFight(e.ItemIndex);

            e.Item = new ListViewItem();
            e.Item.Text = f.Name;
            e.Item.SubItems.Add(f.Zone);
            e.Item.SubItems.Add(f.StartedOn.ToLocalTime().ToString());
            e.Item.SubItems.Add(Utils.FormatNum(f.HP));
            e.Item.SubItems.Add(f.Duration.ToString() + "s");
            e.Item.SubItems.Add(f.Participants.Count + " - " + f.Party);
            fightStatus.TryGetValue(f.ID, out string status);
            e.Item.SubItems.Add(status ?? "-");
        }

        private void lvFights_BeforeLabelEdit(object sender, LabelEditEventArgs e)
        {

        }

        private void lvFights_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(e.Label))
            {
                e.CancelEdit = true;
            }
            else
            {
                var f = GetListViewFight(e.Item);
                f.Name = e.Label;
                // flag as synthetic
                if (f.MobCount == 0)
                    f.MobCount = 1;
            }

            //var item = lvFights.Items[e.Item];
            //var f = item.Tag as FightInfo;
            //if (String.IsNullOrWhiteSpace(e.Label))
            //{
            //    e.CancelEdit = true;
            //}
            //else
            //{
            //    f.Name = e.Label;
            //}
            //lvFights.LabelEdit = false;
        }

        private void lvFights_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                btnUpload.PerformClick();
            }
        }

        private void lvFights_VirtualItemsSelectionRangeChanged(object sender, ListViewVirtualItemsSelectionRangeChangedEventArgs e)
        {
            // this only fires for shift+arrow selects
            lvFights_ItemSelectionChanged(sender, null);
        }

        private void lvFights_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            // this only fires for mouse selects
            btnCombine.Text = lvFights.SelectedIndices.Count + " → 1";
            btnCombine.Enabled = lvFights.SelectedIndices.Count > 1 && lvFights.SelectedIndices.Count < 500;
        }

        private void lvFights_Click(object sender, EventArgs e)
        {
            //var f = lvFights.SelectedItems[0].Tag as FightInfo;
            //lnkSelectDate.Text = f.StartedOn.ToLocalTime().ToShortDateString();
            //lnkSelectZone.Text = f.Zone;
        }

        private void lnkSelectDate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (lvFights.SelectedItems.Count == 0)
                return;

            var copy = lvFights.SelectedItems[0].Tag as FightInfo;
            lvFights.SelectedItems.Clear();
            foreach (var item in lvFights.Items.OfType<ListViewItem>())
            {
                var f = item.Tag as FightInfo;
                //item.Selected = f.StartedOn.ToShortDateString() == lnkSelectDate.Text;
                item.Selected = f.StartedOn.ToLocalTime().Date == copy.StartedOn.ToLocalTime().Date && f.Party == copy.Party && f.MobCount == 0;
            }
        }

        private void lnkSelectZone_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (lvFights.SelectedItems.Count == 0)
                return;

            var copy = lvFights.SelectedItems[0].Tag as FightInfo;
            lvFights.SelectedItems.Clear();
            foreach (var item in lvFights.Items.OfType<ListViewItem>())
            {
                var f = item.Tag as FightInfo;
                //item.Selected = f.StartedOn.ToShortDateString() == lnkSelectDate.Text;
                item.Selected = f.StartedOn.ToLocalTime().Date == copy.StartedOn.ToLocalTime().Date && f.Party == copy.Party && f.Zone == copy.Zone && f.MobCount == 0;
            }
        }

        private FightInfo GetListViewFight(int index)
        {
            if (fightSearchList != null)
                return fightSearchList[index];

            return fightList[index];
        }

        private List<FightInfo> GetSelectedListViewFights()
        {
            var list = new List<FightInfo>();

            foreach (var index in lvFights.SelectedIndices.OfType<int>())
            {
                list.Add(GetListViewFight(index));
            }

            return list;
        }

        /// <summary>
        /// Start monitoring a log file from a background task.
        /// </summary>
        private async void WatchFile(string path)
        {
            if (!File.Exists(path))
                return;

            if (String.IsNullOrEmpty(LogParser.GetPlayerFromFileName(path)))
            {
                LogInfo($"Cannot open {path} because it doesn't use the standard naming convention.");
                return;
            }

            config.Write("filename", path);
            LogInfo("Loading " + path);

            // always disable auto uploads when opening a file to avoid accidentally upload a lot of data
            chkAutoGroup.Checked = chkAutoGroup.Enabled = false;
            chkAutoRaid.Checked = chkAutoRaid.Enabled = false;

            // we don't know where to find the spell_us.txt file until we open a log file, we can then make a guess
            // spells should be one folder down from the log folder
            if (!spells.IsReady)
            {
                var spellpath = Path.GetDirectoryName(path) + @"\..\spells_us.txt";
                if (File.Exists(spellpath))
                {
                    LogInfo("Loading " + spellpath);
                    spells.Load(spellpath);
                }
                else
                {
                    LogInfo("spells_us.txt not found. Class detection will not work properly.");
                }
            }

            // cancel previous log parsing task
            if (cancellationSource != null)
            {
                cancellationSource.Cancel();
                //await Task.Delay(600);
                // reset the tracker by creating a new one
                fightTracker.OnFightFinished -= LogFight;
                fightTracker = new FightTracker();
                fightTracker.OnFightFinished += LogFight;
                fightList.Clear();
                fightSearchList = null;
                lvFights.VirtualListSize = 0;
                ignoredCount = 0;
                charTracker = new CharTracker();
            }

            // read roster files to assist CharTracker
            // this would probably be more useful if it ran in the background to wait on new roster files
            var roster = new RosterParser();
            var raids = new DirectoryInfo(Path.GetDirectoryName(path)).GetFiles(@"..\RaidRoster_*.txt").Where(x => x.CreationTime > DateTime.Today.AddDays(-7));
            foreach (var f in raids)
            {
                //LogInfo("Loading " + f.FullName); // spammy
                roster.Load(f.FullName);
            }
            foreach (var who in roster.Chars)
            {
                fightTracker.HandleEvent(who);
                charTracker.HandleEvent(who);
            }

            // this progress event is a threadsafe way for the background task to update the GUI
            // https://stackoverflow.com/questions/661561/how-do-i-update-the-gui-from-another-thread/18033198#18033198
            var progress = new Progress<LogReaderStatus>(p =>
            {
                //toolStripStatusLabel1.Text = p.Percent.ToString("P0") + " " + p.Notes;
                toolStripStatusLabel1.Text = p.Percent.ToString("P0");
                statusStrip1.Refresh();
                chkAutoGroup.Enabled = p.Percent > 0.9;
                chkAutoRaid.Enabled = p.Percent > 0.9;
                // the log reader reports events in batches so there may be a few events queued up
                while (eventQueue.TryDequeue(out LogEvent e))
                {
                    fightTracker.HandleEvent(e);
                    lootTracker.HandleEvent(e);
                    charTracker.HandleEvent(e);
                }
            });
            cancellationSource = new CancellationTokenSource();
            var reader = new BackgroundLogReader(path, cancellationSource.Token, progress, eventQueue);
            await Task.Factory.StartNew(reader.Run, TaskCreationOptions.LongRunning);
            //await Task.Factory.StartNew(reader.Run, cancellationToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            // this log message can occur after the form has been disposed
            //LogInfo("Closing " + path);
        }

        public void LogFight(FightInfo f)
        {
            // don't include trash thats killed too fast
            //if ((f.Duration < 30 && f.HP < 1_000_000) || f.HP < 1000)
            if (f.Duration < 15 || f.Target.InboundHitCount < 20 || f.HP < 1000)
            {
                //LogInfo("Ignoring " + f.Name);
                ignoredCount += 1;
                return;
            }

            fightList.Insert(0, f);

            if (fightSearchList != null)
            {
                fightSearchList.Insert(0, f);
                //fightSearchList.Add(f);
                lvFights.VirtualListSize = fightSearchList.Count;
            }
            else
            {
                lvFights.VirtualListSize = fightList.Count;
            }

            if (f.MobCount > 0)
            {
                // select the new item
                lvFights.SelectedIndices.Clear();
                lvFights.SelectedIndices.Add(0);
                lvFights.EnsureVisible(0);

                // put it in edit mode
                //lvFights.LabelEdit = true;
                //lvFights.Focus();
                //SendKeys.Send("{F2}");
                //item.BeginEdit();
                //return;
            }
            else if (lvFights.SelectedIndices.Count > 0)
            {
                // if inserting the fight at index 0, we need to shift all the SelectedIndices down by one
                var selected = lvFights.SelectedIndices.OfType<int>().ToList();
                lvFights.SelectedIndices.Clear();
                foreach (var i in selected)
                    lvFights.SelectedIndices.Add(i + 1);
            }


            toolStripStatusLabel2.Text = lvFights.Items.Count + " fights kept (" + ignoredCount + " ignored)";


            if (chkAutoGroup.Checked && f.Party == "Group")
                UploadFight(f);

            if (chkAutoRaid.Checked && f.Party == "Raid")
                UploadFight(f);
        }

        public void LogLoot(LootInfo l)
        {
            lootList.Add(l);
            //LogInfo($"Looted {l.Item} from {l.Source} in {l.Zone}");
        }

        public void LogInfo(string text)
        {
            textLog.AppendText(text + "\r\n");
        }

        public async void UploadFight(FightInfo f)
        {
            if (!uploader.IsReady)
                return;

            fightStatus[f.ID] = "Uploading...";
            lvFights.Invalidate();

            var result = await uploader.UploadFight(f);

            fightStatus[f.ID] = result ? "Uploaded" : "Failed";
            lvFights.Invalidate();
        }

        public static void SetDoubleBuffered(Control control, bool enable)
        {
            // https://stackoverflow.com/questions/87795/how-to-prevent-flickering-in-listview-when-updating-a-single-listviewitems-text/3886695
            var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferPropertyInfo.SetValue(control, enable, null);
        }

    }
}
