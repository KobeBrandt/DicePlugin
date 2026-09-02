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
        private Int32 fontSizeChanger = 3;

        public CustomDie()
        {
            // Set basic properties
            this.Name = "Custom die";
            this.DisplayName = "Custom die";
            this.GroupName = "";
            this.Description = "A dice witch the user can set the value.";

            // Add controls for user configuration
            this.ActionEditor.AddControlEx(
            new ActionEditorSlider(name: "dieSides", labelText: "Dice value:", description: "Set the dice value")
            .SetValues(minimumValue: 1, maximumValue: 100, defaultValue: 20, step: 1)
            .SetFormatString("{0}"));


            this.ActionEditor.ControlValueChanged += this.OnControlValueChanged;
            this.IsWidget = true;
        }

        private void OnControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {
            if (e.ControlName.EqualsNoCase("dieSides"))
            {
                this.dieSides = Int32.Parse(e.ActionEditorState.GetControlValue("dieSides"));

                // Update display name based on user input
                e.ActionEditorState.SetDisplayName($"D{this.dieSides}{Environment.NewLine}");
                this.ActionImageChanged();
            }
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            if (actionParameters.TryGetString("dieSides", out var dieSides))
            {
                this.dieSides = Int32.Parse(dieSides);

                this.rollInProgress();
                this.currentRoll = this.random.Next(this.dieSides) + 1;
                PluginLog.Info($"Throw diceSides is {this.currentRoll}");
                this.rollDone();
                this.ActionImageChanged();
                return true;
            }

            return false;
        }

        private void rollInProgress()
        {
            this.fontSizeChanger = 5;
            this.ActionImageChanged();
            Thread.Sleep(250);
        }
        private void rollDone()
        {
            this.fontSizeChanger = 3;
            this.ActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameter, Int32 imageWidth, Int32 imageHeight)
        {
            
            var fontSize = imageWidth / this.fontSizeChanger;
            using var bitmapBuilder = new BitmapBuilder(imageWidth, imageHeight);
            if (this.currentRoll != 0)
            {
                PluginLog.Info($"Changing image");
                bitmapBuilder.DrawText($"D{this.dieSides}{Environment.NewLine}{this.currentRoll}", fontSize: fontSize, lineHeight: fontSize);
                PluginLog.Info($"D{this.dieSides}{Environment.NewLine}{this.currentRoll}");
            }
            else
            {
                bitmapBuilder.DrawText($"D{this.dieSides}{Environment.NewLine}", fontSize: fontSize, lineHeight: fontSize);
            }
            return bitmapBuilder.ToImage();
        }
    }

}