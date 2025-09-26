namespace Loupedeck.DicePlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class D6 : PluginDynamicCommand
    {
        private Random random = new Random();
        private int hand;
        private const int value = 6;

        // Initializes the command class.
        public D6()
            : base(displayName: $"D{value}", description: $"Rolls a {value}", groupName: "Dice")
        {
            base.IsWidget = true;
        }

        // This method is called when the user executes the command.
        protected override void RunCommand(String actionParameter)
        {
            this.hand = random.Next(value) + 1;
            this.ActionImageChanged(); // Notify the plugin service that the command display name and/or image has changed.
            PluginLog.Info($"Throw value is {this.hand}"); // Write the current counter value to the log file.
        }

        // This method is called when Loupedeck needs to show the command on the console or the UI.
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {
                //bitmapBuilder.DrawText();
                var lineHeight = 32;
                var fontSize = 32;
                // Shadow (2px Offset)
                bitmapBuilder.DrawText($"D{value}{Environment.NewLine}{this.hand}", fontSize: fontSize, lineHeight: lineHeight);




                return bitmapBuilder.ToImage();
            }
        }
    }
}