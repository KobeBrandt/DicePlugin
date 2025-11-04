namespace Loupedeck.DicePlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class CustomDie : ActionEditorCommand
    {
        private Int32 dieSides;
        private Random random = new Random();
        private Int32 currentRoll = 0;
        private Int32 font = 32;

        public CustomDie()
        {
            // Set basic properties
            this.Name = "Custom die";
            this.DisplayName = "Custom die";
            this.GroupName = "Dice";
            this.Description = "A dice ofwitch the user can set the value.";

            // Add controls for user configuration
            this.ActionEditor.AddControlEx(
new ActionEditorSlider(name: "dieSides", labelText: "Dice value:", description: "Set the dice value")
.SetValues(minimumValue: 1, maximumValue: 1000, defaultValue: 20, step: 1)
.SetFormatString("{0}"));


            this.ActionEditor.ControlValueChanged += this.OnControlValueChanged;
        }

        private void OnControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {
            if (e.ControlName.EqualsNoCase("dieSides"))
            {
                dieSides = Int32.Parse(e.ActionEditorState.GetControlValue("dieSides"));

                // Update display name based on user input
                e.ActionEditorState.SetDisplayName($"D{this.dieSides}{Environment.NewLine}{this.currentRoll}");
            }
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            if (actionParameters.TryGetString("dieSides", out var dieSides))
            {
                this.dieSides = Int32.Parse(dieSides);

                this.rollInProgress();
                this.currentRoll = this.random.Next(this.dieSides) + 1;
                this.rollDone();
                PluginLog.Info($"Throw diceSides is {this.currentRoll}");

                return true;
            }

            return false;
        }

        private void rollInProgress()
        {
            this.font = 16;
            this.ActionImageChanged();
            Thread.Sleep(250);
        }
        private void rollDone()
        {
            this.font = 32;
            this.ActionImageChanged();
        }

        protected BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {

                bitmapBuilder.DrawText($"D{this.dieSides}{Environment.NewLine}{this.currentRoll}", fontSize: this.font, lineHeight: this.font);

                return bitmapBuilder.ToImage();
            }
        }
    }

}