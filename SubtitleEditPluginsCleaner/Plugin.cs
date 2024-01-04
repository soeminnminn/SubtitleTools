using System;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.PluginLogic
{
    public class Cleaner : IPlugin
    {
        string IPlugin.Name
        {
            get => "Clean All";
        }

        string IPlugin.Text
        {
            get => "Clean All";
        }

        decimal IPlugin.Version
        {
            get => 0.1M;
        }

        string IPlugin.Description
        {
            get => "Clean All current subtitle.";
        }

        // Can be one of these: file, tool, sync, translate, spellcheck
        string IPlugin.ActionType
        {
            get => "tool";
        }

        string IPlugin.Shortcut
        {
            get => string.Empty;
        }

        public string DoAction(Form parentForm, string srtText, double frameRate, string uiLineBreak, string file, string videoFile, string rawText)
        {
            string text = srtText.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("No subtitle loaded", parentForm.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return string.Empty;
            }

            SubtitleTools.ISubtitle subtitle = new SubtitleTools.Subtitle();
            if (subtitle.Parse(text))
            {
                SubtitleTools.Cleaner cleaner = new SubtitleTools.Cleaner();
                cleaner.Clean(ref subtitle);
                return subtitle.ToString();
            }

            return text;
        }
    }
}