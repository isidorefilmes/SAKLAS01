using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Speech.Recognition; // adding speech recognition
using System.Speech.Synthesis; // adding speech synthesis
using System.IO; // working with files
using AIMLbot;

namespace SAKLAS01
{
    public partial class Form1 : Form
    {
        private SpeechRecognitionEngine engine; //
        private SpeechSynthesizer speaker; //speech synthesis
        private Bot bot;
        private User user;

        // forms
        private Browser browser;
        
        private Dictionary<string, string> Commands; // key-value
        private Dictionary<string, string> OpenCommands; // open browser
        private Dictionary<string, string> Sites;

        private List<string> DefaultCommands;

        private string lastCommand = string.Empty;

        private Handler handler;

        public Form1()
        {
            InitializeComponent();
        }
        
        public void LoadSpeech()
        {
            try
            {
                bot = new Bot();
                bot.isAcceptingUserInput = false;
                bot.loadSettings();
                user = new User("isidore", bot);
                bot.loadAIMLFromFiles();
                bot.isAcceptingUserInput = true;

                speaker = new SpeechSynthesizer(); // creating a instance

                // get the voices

                foreach(InstalledVoice voice in speaker.GetInstalledVoices())
                {
                    this.comboBox1.Items.Add(voice.VoiceInfo.Name);
                }

                engine = new SpeechRecognitionEngine(); // creating instance of a voice recognition 

                engine.SetInputToDefaultAudioDevice(); // set the microphone

                engine.LoadGrammar(new DictationGrammar()); // ADD the dictation grammar

                engine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(rec); // the recognition event

                engine.AudioLevelUpdated += new EventHandler<AudioLevelUpdatedEventArgs>(audioLevel);

                speaker.SpeakStarted += new EventHandler<SpeakStartedEventArgs>(speaker_SpeakStarted);
                speaker.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(speaker_SpeakCompleted);

                engine.RecognizeAsync(RecognizeMode.Multiple); // Starts recognition

                #region Loading key-value into the dictionary

                Commands = new Dictionary<string, string>();

                handler = new Handler(Commands);

                StreamReader reader = new StreamReader("cmds.txt");

                while(reader.Peek() >=0 )
                {
                    string line = reader.ReadLine();

                    var parts = line.Split('|'); // parts[0] key parts[1] value

                    Commands.Add(parts[0], parts[1]); // the commands into the Dicts
                }
                #endregion

                #region Create  the commands Grammar

                Grammar commandsGrammar = new Grammar(new GrammarBuilder(new Choices(Commands.Keys.ToArray())));
                commandsGrammar.Name = "cmds";
                engine.LoadGrammar(commandsGrammar);

                #endregion

                #region Load the open commands

                OpenCommands = new Dictionary<string, string>();
                StreamReader readerOpenCommands = new StreamReader("open.txt");
                while(readerOpenCommands.Peek() >= 0)
                {
                    string line = readerOpenCommands.ReadLine();
                    var parts = line.Split('|');

                    OpenCommands.Add(parts[0], parts[1]);
                }


                #endregion

                #region Create the open commands grammar

                Grammar opencommands = new Grammar(new GrammarBuilder(new Choices(OpenCommands.Keys.ToArray())));
                opencommands.Name = "open";

                engine.LoadGrammar(opencommands);

                #endregion

                #region Load sites into dic

                Sites = new Dictionary<string, string>();
                StreamReader readerSites = new StreamReader("sites.txt");

                while (readerSites.Peek() >= 0)
                {
                    string line = readerSites.ReadLine();
                    var parts = line.Split('|');
                    Sites.Add(parts[0], parts[1]);
                }

                Grammar sites = new Grammar(new GrammarBuilder(new Choices(Sites.Keys.ToArray())));
                sites.Name = "sites";

                engine.LoadGrammar(sites);

                #endregion

                #region Create the default command LIST

                DefaultCommands = new List<string>();

                DefaultCommands.Add("close");
                DefaultCommands.Add("minimize window");
                DefaultCommands.Add("maximize window");
                DefaultCommands.Add("show window");
                DefaultCommands.Add("show window");

                Grammar defaultCmds = new Grammar(new GrammarBuilder(new Choices(DefaultCommands.ToArray())));
                defaultCmds.Name = "defaultCmds";

                engine.LoadGrammar(defaultCmds);

                #endregion

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadSpeech();
        }
        private void speaker_SpeakStarted(object s, SpeakStartedEventArgs e)
        {
            engine.RecognizeAsyncStop();
        }

        private void speaker_SpeakCompleted(object s, SpeakCompletedEventArgs e)
        {
            engine.RecognizeAsync(RecognizeMode.Multiple);
        }

        private void audioLevel(object c, AudioLevelUpdatedEventArgs e)
        {
            this.progressBar1.Maximum = 100;
            this.progressBar1.Value = e.AudioLevel;
        }
        private void rec (object c, SpeechRecognizedEventArgs e)
        {
            string speech = e.Result.Text.ToLower();

            if (e.Result.Confidence > 0.2f)
            {
                string answer = string.Empty;
                switch (e.Result.Grammar.Name)
                {
                    case "cmds":
                        handler.Handle(speech);
                        answer = handler.Response();
                        break;
                    case "open":
                        answer = HandleOpenCommands(speech);
                        break;
                    case "sites":
                        answer = HandleSites(speech);
                        break;
                    case "defaultCmds":
                        answer = HandleDefaultCommands(speech);
                        break;
                    default:
                        if (speech.StartsWith("looking for"))
                        {
                            if (browser == null)
                            {
                                answer = "The browser is not opened.";
                            }
                            else
                            {
                                speech = speech.Replace("search for", " ");
                                answer = "Searching for " + speech;

                                speech = speech.Trim();
                                speech = speech.Replace(" ", "+");
                                browser.LoadPage("http://www.google.com/search?q=" + speech);
                            }
                        }
                        else if (speech.StartsWith("movie"))
                        {
                            if (browser == null)
                            {
                                answer = "The browser is not opened.";
                            }
                            else
                            {
                                speech = speech.Replace("movie", " ");
                                answer = "Searching for " + speech;

                                speech = speech.Trim();
                                speech = speech.Replace(" ", "+");
                                browser.LoadPage("http://www.youtube.com/search?q=" + speech);
                            }
                        }
                        else
                        {
                            answer = GetResponse(speech);
                        }
                        break;
                }
                this.label1.Text = "YOU: " + speech;
                speaker.SpeakAsync(answer);
                lastCommand = speech;
            }
        }

        private string HandleDefaultCommands(string input)
        {
            string response = string.Empty;
            switch (input)
            {
                case "close":
                    response = "closing.";
                    this.Close();
                    break;
                case "minimize window":
                    if (this.WindowState == FormWindowState.Normal || this.WindowState == FormWindowState.Maximized)
                    {
                        response = "minimizing.";
                        this.WindowState = FormWindowState.Minimized;
                    }
                    else
                    {
                        response = "the window is already miximized.";
                    }

                    break;
                case "maximize window":
                    if(this.WindowState == FormWindowState.Normal || this.WindowState == FormWindowState.Minimized)
                    {
                        response = "maximizing.";
                        this.WindowState = FormWindowState.Maximized;
                    }
                    else
                    {
                        response = "the window is already maximized.";
                    }
                    break;
                case "show window":
                    if (this.WindowState == FormWindowState.Minimized || this.WindowState == FormWindowState.Maximized)
                    {
                        response = "showing window.";
                        this.WindowState = FormWindowState.Normal;
                    }
                    else
                    {
                        response = "the window is already showed";
                    }
                    break;
            }
            return response;
        }
        


        private string HandleSites(string input)
        {
            string response = string.Empty;
            if (browser == null)
            {
                response = "the browser is not opened.";
            }
            else
            {
                browser.LoadPage(Sites[input]);
                response = "Ok sir, loading " + input;
             
           
            }
            return response;
        }
        

        private string HandleOpenCommands(string input)
        {
            string response = string.Empty;
            string openType = string.Empty;
            try
            {
                openType = OpenCommands[input];
            }
            catch
            {
                openType = "None";
            }
            switch (openType)
            {
                case "Browser":
                    if (browser == null)
                    {
                        browser = new Browser();
                        browser.Show();
                        response = "opening the browser";
                    }
                    else if(browser.IsDisposed == true)
                    {
                        browser = new Browser();
                        browser.Show();
                        response = "opening the browser";
                    }
                    else
                    {
                        response = "the browser is already opened.";
                    }
                    
                    break;
            }
            return response;
            

        }
        private string GetResponse(string input)
        {
            Request request = new Request(input, user, bot);
            Result result = bot.Chat(request);
            return result.Output;
        }


        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                speaker.SelectVoice(this.comboBox1.SelectedItem.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }
    }
}
