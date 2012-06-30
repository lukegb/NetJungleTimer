using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;

namespace NetJungleTimer.UI
{
    class MuseBotNP : IUIElement
    {
        Label musebotNpTextLabel;

        internal MuseBotNP(Label mbNTL)
        {
            musebotNpTextLabel = mbNTL;

            TextBlock tb = new TextBlock();
            tb.Inlines.Add(new Bold(new Run("Nothing playing")));
            mbNTL.Content = tb;
        }

        public bool GotKey(KeyboardManager.KMKey keyPressed)
        {
            // nothing yet
            return false;
        }

        private Tuple<String, String> SplitStr(String input, String splitOn)
        {
            var splindex = input.IndexOf(splitOn);
            var subs = input.Substring(0, splindex);
            var ends = input.Substring(splindex + splitOn.Length);
            return Tuple.Create(subs, ends);
        }

        public void GotMessage(String message)
        {
            var stripMessage = message.Substring(message.IndexOf(" ") + 1);

            TextBlock tb = new TextBlock();

            if (stripMessage.IndexOf("!__BY__!") == -1 || stripMessage.IndexOf("!__IN__!") == -1)
            {
                tb.Inlines.Add(new Bold(new Run(stripMessage)));
            }
            else
            {
                bool ShouldShowAlbum = stripMessage.Length < 120;
                bool ShouldShowArtist = stripMessage.Length < 100;

                Tuple<String, String> tmpTuple;

                tmpTuple = SplitStr(stripMessage, "!__BY__!");
                tb.Inlines.Add(new Bold(new Run(tmpTuple.Item1)));

                if (ShouldShowArtist)
                {
                    tb.Inlines.Add(new Run(" by "));
                    tmpTuple = SplitStr(tmpTuple.Item2, "!__IN__!");
                    tb.Inlines.Add(new Bold(new Run(tmpTuple.Item1)));

                    if (ShouldShowAlbum)
                    {
                        tb.Inlines.Add(new Run(" in "));
                        tb.Inlines.Add(new Bold(new Run(tmpTuple.Item2)));
                    }
                }
            }
            musebotNpTextLabel.Content = tb;
        }

        // no-ops
        public void UpdateComponent(DateTime now) { }
        public void SyncData() { }
    }
}
