using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;
using Terminal.Gui;
using EQLogParser;
using EQLogParser.Helpers;

/*

   __      _               __             
  /__\ ___| |_ _ __ ___   / /  ___   __ _ 
 / \/// _ \ __| '__/ _ \ / /  / _ \ / _` |
/ _  \  __/ |_| | | (_) / /__| (_) | (_| |
\/ \_/\___|\__|_|  \___/\____/\___/ \__, |
                                    |___/ 
                                                      
A DOS-like log parser for a game that's barely out of the DOS era.

*/

class RetroLog
{
    const string settingsPath = "retrolog.json";
    static Settings settings = new Settings();


    static BackgroundLogReader logReader;
    static ConcurrentQueue<FightInfo> fightQueue = new ConcurrentQueue<FightInfo>(); // background processing queue
    static List<FightInfoRow> fightList = new List<FightInfoRow>(); // all the fights
    static List<FightInfoRow> fightListSource = null; // currently displayed fights 
    static CancellationTokenSource cancellationSource = null;

    static void Main(string[] args)
    {
        LoadSettings();

        Application.Init();
        Colors.Base.Normal = Application.Driver.MakeAttribute(Color.Green, Color.Black);
        Colors.ColorSchemes.Add("Highlight", new ColorScheme() { Normal = Application.Driver.MakeAttribute(Color.BrightGreen, Color.Black) });


        var win = new Window()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        Application.Top.Add(win);
        //var win = Application.Top;

        var searchLabel = new Label()
        {
            X = 0,
            Y = 0,
            Text = "Search:",
        };
        win.Add(searchLabel);
        var searchField = new TextField()
        {
            X = Pos.Right(searchLabel) + 1,
            Y = 0,
            Width = Dim.Sized(20),
            Height = 1,
        };
        win.Add(searchField);
        var searchCount = new Label()
        {
            X = Pos.Right(searchField) + 1,
            Y = 0,
        };
        win.Add(searchCount);

        var alwaysLabel = new Label()
        {
            X = Pos.Right(searchCount) + 3,
            Y = 0,
            Text = "Always Show:",
        };
        win.Add(alwaysLabel);
        var alwaysField = new TextField()
        {
            X = Pos.Right(alwaysLabel) + 1,
            Y = 0,
            Width = Dim.Sized(20),
            Height = 1,
            TabStop = false,
        };
        win.Add(alwaysField);

        var modeLabel = new Label()
        {
            X = Pos.Right(alwaysField) + 3,
            Y = 0,
            Text = "Mode:",
        };
        win.Add(modeLabel);
        var modeRadio = new RadioGroup()
        {
            X = Pos.Right(modeLabel) + 1,
            Y = 0,
            Width = Dim.Sized(20),
            RadioLabels = new NStack.ustring[] { "Total  ", "DPS  ", "%Total  " },
            HorizontalSpace = 2,
            DisplayMode = DisplayModeLayout.Horizontal,
            TabStop = false, // annoying when tabbing between search and fights
            SelectedItem = settings.Mode,
        };
        win.Add(modeRadio);

        //var fightSource = new FightDataSource(fightList);
        //var fightView = new ListView(fightSource)
        var fightView = new ListView(fightList)
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
            //Height = 18,
        };
        win.Add(fightView);

        // the bottom portion of the screen will contain "slots" for individual player information
        var slots = new List<View>();
        for (var i = 0; i < 6; i++)
        {
            var slotView = new FrameView()
            {
                X = 0 + (i * 25),
                Y = Pos.Bottom(fightView) + 1,
                Width = 25,
                Height = Dim.Fill(),
            };
            var slotText = new Label()
            {
                X = 0,
                Y = 0,
                Height = 10,
                Width = Dim.Fill(),
                Text = "..."
            };
            slotView.Add(slotText);
            win.Add(slotView);
            slots.Add(slotText);
        }

        var status = new StatusBar()
        {
            Visible = true,
            Items = new StatusItem[] {
                new StatusItem(Key.Null, "...", () => { }),
                new StatusItem(Key.Null, "...", () => { }),
                new StatusItem(Key.Null, "Alt+Enter to toggle full screen", () => { })
            },
            Text = "Ready..."
        };
        Application.Top.Add(status);

        searchField.KeyDown += (key) =>
        {
            if (key.KeyEvent.Key == Key.Esc)
                searchField.Text = "";
        };

        searchField.TextChanged += (_) =>
        {
            var s = searchField.Text.ToString();
            if (String.IsNullOrEmpty(s))
            {
                fightListSource = null;
                fightView.SetSource(fightList);
                searchCount.Text = "";
            }
            else
            {
                fightListSource = fightList.Where(x => x.IsMatch(s)).ToList();
                fightView.SetSource(fightListSource);
                fightView.OnSelectedChanged();
                searchCount.Text = $"{fightListSource.Count} matches";
            }
        };

        alwaysField.TextChanged += (_) =>
        {
            fightView.OnSelectedChanged();
        };

        modeRadio.SelectedItemChanged += (args) =>
        {
            fightView.OnSelectedChanged();
        };

        fightView.SelectedItemChanged += (args) =>
        {
            var list = fightListSource ?? fightList;

            if (args.Item >= list.Count)
                return;

            var f = list[args.Item].Info;
            //status.Items[2].Title = DateTime.Now.ToString() + " " + f.Name + " " + f.Participants[0].OutboundHitSum;

            var mode = modeRadio.SelectedItem;

            var players = f.Participants.Take(slots.Count).ToList();

            var always = alwaysField.Text.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (always.Length > 0)
            {
                players = f.Participants
                    .Select((x, i) => new { Always = always.Contains(x.Name, StringComparer.InvariantCultureIgnoreCase) && x.OutboundHitSum > 0, Index = i, Player = x })
                    .OrderBy(x => !x.Always).ThenBy(x => x.Index)
                    .Take(slots.Count)
                    .OrderBy(x => x.Index) // restore order after take() keeps the "always" entries
                    .Select(x => x.Player)
                    .ToList();
            }

            for (var i = 0; i < slots.Count; i++)
            {
                slots[i].Text = "";

                if (i >= players.Count)
                    continue;

                var p = players[i];
                if (p.OutboundHitSum == 0)
                    continue;

                var text = new StringBuilder();
                text.AppendLine($"{p.Name} - {p.Class}");
                if (mode == 0)
                    text.AppendLine($"{FightUtils.FormatNum(p.OutboundHitSum)}");
                else if (mode == 1)
                    text.AppendLine($"{FightUtils.FormatNum(p.OutboundHitSum / f.Duration)} DPS");
                else if (mode == 2)
                {
                    text.AppendLine($"{((double)p.OutboundHitSum / f.HP).ToString("P1")} of Total");
                    text.AppendLine($"{((double)p.OutboundHitSum / f.TopHitSum).ToString("P1")} vs Top Player");
                }
                text.AppendLine("---");
                foreach (var dmg in p.AttackTypes)
                {
                    if (mode == 0)
                        text.AppendLine($"{Clip(dmg.Type, 15),-15} {FightUtils.FormatNum(dmg.HitSum),6}");
                    else if (mode == 1)
                        text.AppendLine($"{Clip(dmg.Type, 15),-15} {FightUtils.FormatNum(dmg.HitSum / f.Duration),6}");
                    else if (mode == 2)
                        text.AppendLine($"{Clip(dmg.Type, 15),-15} {((double)dmg.HitSum / p.OutboundHitSum).ToString("P0"),6}");
                }
                text.AppendLine("---");
                foreach (var spell in p.Spells.Where(x => x.HitSum > 0 && x.Type == "hit"))
                {
                    if (mode == 0)
                        text.AppendLine($"{Clip(spell.Name, 15),-15} {FightUtils.FormatNum(spell.HitSum),6}");
                    else if (mode == 1)
                        text.AppendLine($"{Clip(spell.Name, 15),-15} {FightUtils.FormatNum(spell.HitSum / f.Duration),6}");
                    else if (mode == 2)
                        text.AppendLine($"{Clip(spell.Name, 15),-15} {((double)spell.HitSum / p.OutboundHitSum).ToString("P0"),6}");
                }
                slots[i].Text = text.ToString();
                slots[i].SuperView.ColorScheme = always.Contains(p.Name, StringComparer.InvariantCultureIgnoreCase) ? Colors.ColorSchemes["Highlight"] : Colors.Base;
            }
        };

        var path = settings.Path;
        if (args.Length > 1 && File.Exists(args[1]))
            path = args[1];
        if (File.Exists(path))
        {
            alwaysField.Text = LogOpenEvent.FromFileName(path)?.Player ?? "";
            status.Items[2].Title = path;
            _ = OpenLog(path);
            //status.Items[2].Title = "";
        }

        Func<Exception, bool> errorHandler = (Exception e) =>
        {
            MessageBox.ErrorQuery("Error", e.Message + "\n", "OK");
            return true;
        };

        var menu = new MenuBar(new MenuBarItem[] {
            new MenuBarItem ("_File", new MenuItem [] {
                new MenuItem("_Open", "", () => {
                    var open = new OpenDialog();
                    open.AllowedFileTypes = new[] { ".txt" };
                    open.Message = "Select a file starting with: eqlog_";
                    open.LayoutStyle = LayoutStyle.Computed;
                    open.AllowsMultipleSelection = false;
                    if (File.Exists(settings.Path))
                        open.DirectoryPath = Path.GetDirectoryName(settings.Path);
                    Application.Run(open);
                    if (!open.Canceled)
                    {
                        fightView.SelectedItem = 0;
                        path = open.FilePath.ToString();
                        alwaysField.Text = LogOpenEvent.FromFileName(path)?.Player ?? "";
                        status.Items[2].Title = path;
                        _ = OpenLog(path);

                    }
                }),
                new MenuItem ("_Quit", "", () => {
                    Application.RequestStop();
                })
            }),
        });
        Application.Top.Add(menu);

        Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(100), (_) =>
        {
            var count = 0;
            while (fightQueue.TryDequeue(out FightInfo f) && count++ < 50)
            {
                //fightList.Add(f); // oldest to newest
                fightList.Insert(0, new FightInfoRow(f)); // newest to oldest
            }
            if (count > 0)
            {
                status.Items[0].Title = $"{fightList.Count} fights";
                fightView.OnSelectedChanged();
                //status.SetNeedsDisplay();
                //fightView.SetNeedsDisplay();
                if (logReader != null)
                    status.Items[1].Title = $"{logReader.Percent:P0} in {logReader.Elapsed:F1}s";
            }
            return true;
        });
        Application.Run(errorHandler);

        settings.Mode = modeRadio.SelectedItem;
        SaveSettings();
    }

    static async Task OpenLog(string path)
    {
        if (cancellationSource != null)
        {
            cancellationSource.Cancel();
            Thread.Sleep(100);
        }

        // log parser doesn't work without a log owner name
        var open = LogOpenEvent.FromFileName(path);
        if (String.IsNullOrEmpty(open.Player))
            throw new Exception(path + " could not be loaded.");


        settings.Path = path;
        SaveSettings();

        fightList.Clear();
        fightListSource = null;

        var spells = new SpellParser();
        var spellsPath = Path.Combine(Path.GetDirectoryName(path), @"..\spells_us.txt");
        if (File.Exists(spellsPath))
            spells.Load(spellsPath);
        var parser = new LogParser();
        var charTracker = new CharTracker(spells);
        var fightTracker = new FightTracker(spells, charTracker);
        fightTracker.OnFightFinished += fightQueue.Enqueue;
        string last = null;
        Action<string> handler = (string s) =>
        {
            last = s;
            var e = parser.ParseLine(s);
            if (e != null)
            {
                charTracker.HandleEvent(e);
                fightTracker.HandleEvent(e);
            }
        };
        parser.Player = open.Player;
        charTracker.HandleEvent(open);
        fightTracker.HandleEvent(open);
        cancellationSource = new CancellationTokenSource();
        logReader = new BackgroundLogReader(open.Path, cancellationSource.Token, handler);
        try
        {
            await logReader.Start();
        }
        catch (Exception e)
        {
            MessageBox.ErrorQuery("Error", e.Message + "\n" + last, "OK");
        }
    }

    static string Clip(string s, int max)
    {
        if (s.Length > max)
            return s.Substring(0, max - 2).Trim() + "..";
        return s;
    }

    static void LoadSettings()
    {
        if (File.Exists(settingsPath))
        {
            var data = File.ReadAllText(settingsPath);
            settings = JsonSerializer.Deserialize<Settings>(data);
        }
    }

    static void SaveSettings()
    {
        var options = new JsonSerializerOptions()
        {
            WriteIndented = true,
        };
        var data = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(settingsPath, data);
    }

    public class Settings
    {
        public string Path { get; set; }
        public int Mode { get; set; }
    }

    /// <summary>
    /// The simplest way to use the listview control is to provide it a list of items that are displayed via
    /// an object.ToString() conversion -- that's what this class does.
    /// </summary>
    public class FightInfoRow
    {
        public FightInfo Info { get; }

        public FightInfoRow(FightInfo f)
        {
            Info = f;
        }

        public bool IsMatch(string text)
        {
            if (Info.Name.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) != -1)
                return true;
            if (Info.Zone.IndexOf(text, StringComparison.InvariantCultureIgnoreCase) != -1)
                return true;
            return false;
        }

        public override string ToString()
        {
            var f = Info;
            var mob = f.Name;
            if (mob.Length > 25)
                mob = mob.Substring(0, 23).Trim() + "..";
            var zone = f.Zone ?? "Unknown";
            if (zone.Length > 15)
                zone = zone.Substring(0, 13).Trim() + "..";

            // use vertical bar? | 
            var s = $"{mob,-25}  {zone,-15}  {FightUtils.FormatNum(f.HP),8}  {FightUtils.FormatNum(f.HP / f.Duration),6} DPS  {f.Duration,4}s  {FightUtils.FormatDate(f.UpdatedOn)}  {f.Party}: {f.Participants.Count}";

            return s;
        }
    }

    /*
    public class FightDataSource : IListDataSource
    {
        public int Count => Fights.Count;

        public int Length => Fights.Count;

        private List<FightInfo> Fights { get; }

        public FightDataSource(List<FightInfo> fights)
        {
            Fights = fights;
        }

        public bool IsMarked(int item)
        {
            return false;
        }

        public void Render(ListView container, ConsoleDriver driver, bool selected, int item, int col, int line, int width, int start = 0)
        {
            // https://github.com/migueldeicaza/gui.cs/blob/fc1faba7452ccbdf49028ac49f0c9f0f42bbae91/Terminal.Gui/Views/ListView.cs#L433-L461

            var f = Fights[item];
            var mob = f.Name;
            if (mob.Length > 25)
                mob = mob.Substring(0, 23).Trim() + "..";
            var zone = f.Zone;
            if (zone.Length > 15)
                zone = zone.Substring(0, 13).Trim() + "..";

            //e.Item.SubItems.Add(f.Party + ": " + f.Participants.Count);

            // use vertical bar? | 
            var s = $"{mob,-25}  {zone,-15}  {FightUtils.FormatNum(f.HP),8}  {FightUtils.FormatNum(f.HP / f.Duration),6} DPS  {f.Duration,4}s  {FightUtils.FormatDate(f.StartedOn)}";
            container.Move(1, line);
            driver.AddStr(s);

            //driver.SetAttribute(c: ColorScheme.Focus);
        }

        public void SetMark(int item, bool value)
        {
        }

        public IList ToList()
        {
            return Fights;
        }
    }
    */
};