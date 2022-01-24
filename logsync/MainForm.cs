using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EQLogParser;
using EQLogParser.Helpers;

namespace LogSync
{
    public partial class MainForm : Form
    {
        private readonly IConfigAdapter config;
        //private readonly SynchronizationContext syncContext;
        private CancellationTokenSource cancellationSource;
        private SpellParser spells;
        private LogParser parser;
        private ConcurrentQueue<FightInfo> fightsQueue;
        private ConcurrentQueue<LootInfo> lootQueue;
        private List<FightInfo> fightList;
        private List<FightInfo> fightListSearchResults;
        private Dictionary<string, string> fightStatus;
        private List<LootInfo> lootList;
        private Uploader uploader;

        public MainForm()
        {
            InitializeComponent();
            SetDoubleBuffered(lvFights, true);
            config = new RegConfigAdapter();
            //config = new XmlConfigAdapter();
            spells = new SpellParser();
            parser = new LogParser();
            //parser.MinDate = DateTime.Parse("2020-09-27");
            fightsQueue = new ConcurrentQueue<FightInfo>();
            lootQueue = new ConcurrentQueue<LootInfo>();
            fightList = new List<FightInfo>(2000);
            fightStatus = new Dictionary<string, string>();
            lootList = new List<LootInfo>();
            uploader = new Uploader(LogInfo);
            _ = ProcessEventsAsync();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            //LogInfo(Application.ExecutablePath);
            Text = Application.ProductName;

            // refresh the screen before we run tasks that may make it look frozen
            Refresh();

            // open the last log file
            var path = config.Read("filename");
            if (File.Exists(path))
            {
                textLogPath.Text = path;
                WatchFile(path);
            }
            else if (String.IsNullOrEmpty(path))
            {
                var dir = new DirectoryInfo(@"C:/Users/Public/Daybreak Game Company/Installed Games/Everquest/Logs");
                if (dir.Exists)
                    openLogDialog.InitialDirectory = dir.FullName;

                dir = new DirectoryInfo(@"C:/Users/Public/Sony Online Entertainment/Installed Games/Everquest/Logs");
                if (dir.Exists)
                    openLogDialog.InitialDirectory = dir.FullName;
            }

            _ = uploader.Hello(config);
        }

        private async void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // the background task can finish after the form is disposed -- any attempts to update this form will then produce an exception
            // ideally we should be keeping an counter of background tasks and waiting until it returns to 0
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

            if (e.Control && e.KeyCode == Keys.C)
            {
                var f = GetSelectedListViewFights().FirstOrDefault();
                if (f != null)
                {
                    var json = JsonSerializer.Serialize(f, new JsonSerializerOptions() { WriteIndented = true });
                    Clipboard.SetText(json);
                }
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
                // clone the list since we aren't awaiting the response
                var copy = lootList.ToList();
                lootList = new List<LootInfo>();
                _ = uploader.UploadLoot(copy);
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
            {
                AddFight(total);
                lvFights.LabelEdit = true;
                lvFights.Items[0].BeginEdit();
            }
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
                fightListSearchResults = fightList
                    .Where(IsFilterMatch)
                    .ToList();
                lvFights.VirtualListSize = fightListSearchResults.Count;
            }
            else
            {
                fightListSearchResults = null;
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
            e.Item.SubItems.Add(Utils.FormatNum(f.HP / f.Duration));
            e.Item.SubItems.Add(f.Party + ": " + f.Participants.Count);
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
                // reset status to allow another upload
                fightStatus[f.ID] = null;
            }

            lvFights.LabelEdit = false;
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
            var selected = GetSelectedListViewFights();
            if (selected.Count == 1)
                ViewFight(selected[0]);
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
            if (fightListSearchResults != null)
                return fightListSearchResults[index];

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

            if (String.IsNullOrEmpty(LogOpenEvent.GetPlayerFromFileName(path)))
            {
                LogInfo($"Cannot open {path} because it doesn't use the standard naming convention.");
                return;
            }

            // cancel previous log parsing task
            if (cancellationSource != null)
            {
                cancellationSource.Cancel();
                await Task.Delay(600);
            }

            // always disable auto uploads when opening a file to avoid accidentally uploading a lot of data
            chkAutoUpload.Checked = chkAutoUpload.Enabled = false;

            // we don't know where to find the spell_us.txt file until we open a log file
            // spells should be one folder down from the log folder
            if (!spells.IsReady)
            {
                var spellpath = Path.GetDirectoryName(path) + @"\..\spells_us.txt";
                if (File.Exists(spellpath))
                {
                    LogInfo("Loading " + spellpath);
                    spells.Load(spellpath);
                    if (!spells.IsReady)
                        LogInfo("spells_us.txt could not be loaded. Class detection and buff tracking will not work properly.");
                }
                else
                {
                    LogInfo("spells_us.txt not found. Class detection and buff tracking will not work properly.");
                }
            }

            config.Write("filename", path);
            LogInfo("Loading " + path);
            var open = LogOpenEvent.FromFileName(path);
            parser.Player = open.Player;

            // reset the trackers by creating new ones
            var charTracker = new CharTracker(spells);
            charTracker.Files = new FileService(Path.GetDirectoryName(path) + @"\..\");
            charTracker.HandleEvent(open);
            charTracker.ImportPlayers(config.Read("chars:" + open.Server));

            var fightTracker = new FightTracker(spells, charTracker);
            fightTracker.OnFightFinished += x => fightsQueue.Enqueue(x);
            fightTracker.AddTemplateFromResource();
            fightTracker.HandleEvent(open);
            
            var lootTracker = new LootTracker();
            lootTracker.OnLoot += x => lootQueue.Enqueue(x);
            lootTracker.HandleEvent(open);

            // reset UI
            lvFights.VirtualListSize = 0;
            fightList.Clear();
            fightListSearchResults = null;
            toolStripStatusLabel1.Text = "-";
            toolStripStatusLabel2.Text = "-";

            // read roster files to assist player tracking
            if (!String.IsNullOrEmpty(open.Server))
            {
                var files = new DirectoryInfo(Path.GetDirectoryName(path)).Parent
                    .GetFiles("*_" + open.Server + "-*.txt")
                    .Where(x => x.CreationTime > DateTime.Today.AddDays(-14))
                    .Take(20);
                var roster = RosterParser.Load(files);
                foreach (var who in roster)
                    charTracker.HandleEvent(who);
            }

            // this handler runs in a background thread and must be threadsafe
            Action<string> handler = line =>
            {
                var e = parser.ParseLine(line);
                if (e != null)
                {
                    charTracker.HandleEvent(e);
                    fightTracker.HandleEvent(e);
                    lootTracker.HandleEvent(e);
                }
            };

            // this event runs in the main app thread
            // https://stackoverflow.com/questions/661561/how-do-i-update-the-gui-from-another-thread/18033198#18033198
            var progress = new Progress<LogReaderStatus>(p =>
            {
                toolStripStatusLabel1.Text = p.Percent.ToString("P0") + " " + p.Notes;
                //toolStripStatusLabel1.Text = p.Percent.ToString("P0");
                var completed = p.Percent > 0.99;
                chkAutoUpload.Enabled = completed;
            });
            cancellationSource = new CancellationTokenSource();
            var reader = new BackgroundLogReader(path, cancellationSource.Token, progress, handler);
            await reader.Start();
            LogInfo("Closing " + path);
            if (open.Server != null)
            {
                // save a list of players so that we have better information next time we run the parser
                var players = charTracker.ExportPlayers();
                config.Write("chars:" + open.Server, players);
            }

            //await ProcessLogFileAsync(path);

            // this log message can occur after the form has been disposed
            //LogInfo("Closing " + path);
        }

        /*
        /// <summary>
        /// Process log file using task-based async pattern. 
        /// This code is easy to read and doesn't block the UI but runs about 2x slower than a blocking reader.
        /// </summary>
        public async Task ProcessLogFileAsync(string path)
        {
            var timer = Stopwatch.StartNew();
            var log = new LogReader(path);
            var count = 0;
            while (true)
            {
                var line = await log.ReadLineAsync();

                // have we reached the end of the file?
                if (line == null)
                {
                    await Task.Delay(100);
                    continue;
                }

                // update progress
                count += 1;
                if (count % 500 == 0)
                {
                    //await Task.Yield(); // switch to main thread for UI update
                    toolStripStatusLabel2.Text = $"{log.PercentRead:P1} in {timer.Elapsed.TotalSeconds:F1}s";
                    statusStrip1.Refresh();
                }

                // parse line and update trackers
                var e = parser.ParseLine(line);
                if (e != null)
                {
                    tracker.HandleEvent(e);
                }
            }
        }
        */

        /// <summary>
        /// Copy data from the background queues to the main UI.
        /// </summary>
        private async Task ProcessEventsAsync()
        {
            while (true)
            {
                try
                {
                    while (fightsQueue.TryDequeue(out FightInfo f))
                        AddFight(f);

                    while (lootQueue.TryDequeue(out LootInfo l))
                        AddLoot(l);

                }
                catch (Exception ex)
                {
                    LogInfo(ex.Message);
                }

                await Task.Delay(200);
            }
        }

        private void AddFight(FightInfo f)
        {
            if (f.Zone == null)
                f.Zone = "Unknown";

            if (IsTrashMob(f))
                fightStatus[f.ID] = "Trash";

            // ignore mobs we may have hit but blurred, rooted, or otherwise ignored
            if (IsTrashMob(f) && f.Status == FightStatus.Timeout)
            {
                //LogInfo("Ignoring trash: " + f.Name);
                return;
            }

            /*
            // consolidate with recent "trash" mobs
            if (IsTrashMob(f))
            {
                // find the last synthetic "trash" fight that we can consolidate this with
                var trash = fightList.OfType<MergedFightInfo>()
                    .Where(x => x.Zone == f.Zone && x.Name.StartsWith("Trash"))
                    //.Where(x => x.StartedOn <= f.StartedOn) // prevents out of order merges, but creates extra "trash" fights sometimes
                    .Where(x => x.StartedOn >= f.StartedOn.AddMinutes(-5))
                    .FirstOrDefault();

                if (trash == null)
                {
                    trash = new MergedFightInfo();
                    fightList.Insert(0, trash);
                }
                else
                {
                    fightList.Remove(trash);
                    fightList.Insert(0, trash);
                }
                trash.Merge(f);
                trash.Trim();
                trash.Finish();
                trash.Name = $"Trash ({trash.MobCount} mobs)";

                // return without uploading
                return;
            }
            */

            fightList.Insert(0, f);

            if (fightListSearchResults == null)
            {
                lvFights.VirtualListSize = fightList.Count;
            }
            else if (IsFilterMatch(f) || f.MobCount > 1)
            {
                fightListSearchResults.Insert(0, f);
                lvFights.VirtualListSize = fightListSearchResults.Count;
            }
            toolStripStatusLabel2.Text = fightList.Count + " fights";

            if (f.MobCount > 1)
            {
                // if we just combined several fights then clear the selection
                lvFights.SelectedIndices.Clear();
                lvFights.SelectedIndices.Add(0);
                lvFights.EnsureVisible(0);
            }
            else if (lvFights.SelectedIndices.Count > 1)
            {
                // if inserting the fight at index 0, we need to shift all the SelectedIndices down by one
                var selected = lvFights.SelectedIndices.OfType<int>().ToList();
                lvFights.SelectedIndices.Clear();
                foreach (var i in selected)
                    lvFights.SelectedIndices.Add(i + 1);
            }

            if (chkAutoUpload.Checked && f.Status == FightStatus.Killed && !IsTrashMob(f))
            {
                UploadFight(f);
            }
        }

        private void AddLoot(LootInfo l)
        {
            if (l.IsStandardLoot)
                lootList.Add(l);
            //LogInfo($"Looted {l.Item} from {l.Source} in {l.Zone}");
        }

        private void LogInfo(string text)
        {
            textLog.AppendText(text + "\r\n");
        }

        private void ViewFight(FightInfo f)
        {
            //var s = new StringBuilder();
            //var writer = new StringWriter(s);
            //f.WriteNotes(writer);
            //LogInfo(s.ToString());

            lvPlayers.BeginUpdate();
            lvPlayers.Items.Clear();
            var top = f.Participants.Max(x => x.OutboundHitSum);
            // show participants that did damage
            foreach (var p in f.Participants.Where(x => x.OutboundHitSum > 0))
            {
                var item = lvPlayers.Items.Add(p.Name);
                item.SubItems.Add(p.Class);
                item.SubItems.Add(FightUtils.FormatNum(p.OutboundHitSum));
                item.SubItems.Add(((double)p.OutboundHitSum / top).ToString("P0"));
                item.SubItems.Add(p.Duration.ToString() + 's');
                item.SubItems.Add(FightUtils.FormatNum(p.OutboundHitSum / f.Duration));
                //var damage = String.Join(", ", p.AttackTypes.Take(4).Select(x => $"{(double)x.HitSum / p.OutboundHitSum:P0} {x.Type}"));
                var notes = String.Join(", ", p.AttackTypes.Take(4).Select(x => $"{FightUtils.FormatNum(x.HitSum / f.Duration)} {x.Type}"));
                var other = p.AttackTypes.Skip(4).Sum(x => x.HitSum);
                if (other > 0)
                    notes += $", {FightUtils.FormatNum(other / f.Duration)} other";
                if (p.Buffs.Any(x => x.Name == BuffTracker.DEATH))
                    notes = "DIED - " + notes;
                item.SubItems.Add(notes);
            }
            lvPlayers.EndUpdate();
        }

        private async void UploadFight(FightInfo f)
        {
            if (!uploader.IsReady)
                return;

            //var hint = new StringWriter();
            //f.DumpShort(hint);
            //LogInfo(hint.ToString());

            fightStatus[f.ID] = "Uploading...";
            lvFights.Invalidate();

            var result = await uploader.UploadFight(f);

            fightStatus[f.ID] = result ? "Uploaded" : "Failed";
            lvFights.Invalidate();
        }

        private bool IsTrashMob(FightInfo f)
        {
            //var zoneAvg = fightList.Take(20).Where(x => x.Zone == f.Zone && x.Status == FightStatus.Killed).Select(x => x.HP).DefaultIfEmpty(0).Average();
            //LogInfo("Avg: " + zoneAvg);
            //if (zoneAvg > 0 && f.HP > zoneAvg * 0.5)
            //    return false;

            return f.IsTrash;
        }

        private bool IsFilterMatch(FightInfo f)
        {
            return f.Name.Contains(textSearch.Text, StringComparison.OrdinalIgnoreCase)
                || f.Zone.Contains(textSearch.Text, StringComparison.OrdinalIgnoreCase)
                || f.Participants.Any(x => x.Name.StartsWith(textSearch.Text, StringComparison.OrdinalIgnoreCase))
                ;
        }

        private static void SetDoubleBuffered(Control control, bool enable)
        {
            // https://stackoverflow.com/questions/87795/how-to-prevent-flickering-in-listview-when-updating-a-single-listviewitems-text/3886695
            var doubleBufferPropertyInfo = control.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            doubleBufferPropertyInfo.SetValue(control, enable, null);
        }
    }
}
