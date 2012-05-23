using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Speech.Synthesis;
using System.IO;
using System.Media;
using System.Threading;
using NAudio.Wave;

namespace NetJungleTimer
{

    class TextToSpeech : IDisposable
    {
        class TextToSpeechCache
        {
            public MemoryStream WavStream;
            public TimeSpan Length;
            public bool Ready = false;

            internal void UpdateLength()
            {
                this.WavStream.Seek(0, SeekOrigin.Begin);
                var wavReader = new WaveFileReader(this.WavStream);
                Length = wavReader.TotalTime;
                Ready = true;
            }
        }

        const string SPEECH_WILL_BE_UP = "{0} will be up in {1} seconds.";
        const string SPEECH_IS_NOW_UP = "{0} is up!";

        SpeechSynthesizer synth = new SpeechSynthesizer();
        bool hasDisposed = false;

        Dictionary<string, TextToSpeechCache> cachedSpeech = new Dictionary<string, TextToSpeechCache>();

        Thread precacheThread;
        Queue<string> precachePhrases = new Queue<string>();
        
        private static readonly Lazy<TextToSpeech> _instance = new Lazy<TextToSpeech>(() => new TextToSpeech());

        public bool Enabled = true;

        public static TextToSpeech Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        private TextToSpeech()
        {
            ThreadStart precacheThreadStart = delegate()
            {
                var myQueueEnumerator = precachePhrases.GetEnumerator();
                while (myQueueEnumerator.MoveNext())
                {
                    PrecachePhraseInThread(myQueueEnumerator.Current);
                }
            };
            precacheThread = new Thread(precacheThreadStart);
        }

        ~TextToSpeech()
        {
            Dispose(false);
        }

        internal void PrecachePhrase(string what)
        {
            if (cachedSpeech.ContainsKey(what) || precachePhrases.Contains(what))
                return;

            precachePhrases.Enqueue(what);

            if (!precacheThread.IsAlive)
                precacheThread.Start();
        }

        internal void PrecachePhraseInThread(string what)
        {
            if (cachedSpeech.ContainsKey(what))
                return;

            MemoryStream ms = new MemoryStream();
            TextToSpeechCache ttsc = new TextToSpeechCache();
            ttsc.WavStream = ms;
            cachedSpeech.Add(what, ttsc);
            synth.SetOutputToWaveStream(ms);
            synth.Speak(what);
            ttsc.UpdateLength();
        }

        internal void PrecacheTimer(UI.NetworkedTimer nt)
        {
            PrecachePhrase(String.Format(SPEECH_IS_NOW_UP, nt.context.ChatMessageComplete));
            PrecachePhrase(String.Format(SPEECH_WILL_BE_UP, nt.context.ChatMessageComplete, UI.NetworkedTimer.PRE_WARNING));
        }

        internal void TellTimerExpiry(UI.NetworkedTimer nt)
        {
            string sayWhat = String.Format(SPEECH_IS_NOW_UP, nt.context.ChatMessageComplete);
            Say(sayWhat);
        }

        internal void TellTimerFinalCountdown(UI.NetworkedTimer nt)
        {
            string sayWhat = String.Format(SPEECH_WILL_BE_UP, nt.context.ChatMessageComplete, UI.NetworkedTimer.PRE_WARNING);
            Say(sayWhat);
        }

        internal TimeSpan TimerExpiryPreTime(UI.NetworkedTimer nt)
        {
            string sayWhat = String.Format(SPEECH_IS_NOW_UP, nt.context.ChatMessageComplete);
            if (!cachedSpeech.ContainsKey(sayWhat))
                return TimeSpan.MinValue;

            while (!cachedSpeech[sayWhat].Ready)
            {
                Thread.Sleep(100);
            }

            return cachedSpeech[sayWhat].Length;
        }

        internal TimeSpan TimerFinalCountdownPreTime(UI.NetworkedTimer nt)
        {
            string sayWhat = String.Format(SPEECH_WILL_BE_UP, nt.context.ChatMessageComplete, UI.NetworkedTimer.PRE_WARNING);
            if (!cachedSpeech.ContainsKey(sayWhat))
                return TimeSpan.MinValue;

            while (!cachedSpeech[sayWhat].Ready)
            {
                Thread.Sleep(100);
            }

            return cachedSpeech[sayWhat].Length;
        }

        internal void Say(string what)
        {
            if (!Enabled)
                return;

            if (cachedSpeech.ContainsKey(what))
            {
                var thisMs = cachedSpeech[what];
                if (!thisMs.Ready)
                    return;
                Console.WriteLine(thisMs.Length);
                thisMs.WavStream.Seek(0, SeekOrigin.Begin);
                var sp = new SoundPlayer(thisMs.WavStream);
                sp.Play();
                sp.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (hasDisposed)
                return;

            if (synth != null)
                synth.Dispose();
            synth = null;

            hasDisposed = true;
        }
    }
}
