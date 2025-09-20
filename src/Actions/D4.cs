namespace Loupedeck.DicePlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class D4 : PluginDynamicCommand
    {
        //private readonly String _imageResourcePath1;

        private Random random = new Random();
        private int hand;
        private const int value = 4;

        // Initializes the command class.
        public D4()
            : base(displayName: $"D{value}", description: $"Rolls a {value}", groupName: "Dice")
        {
            //this._imageResourcePath1 = PluginResources.FindFile("Images/D4/1.png");
        }

        // This method is called when the user executes the command.
        protected override void RunCommand(String actionParameter)
        {
            this.hand = random.Next(value) + 1;
            this.ActionImageChanged(); // Notify the plugin service that the command display name and/or image has changed.
            PluginLog.Info($"Throw value is {this.hand}"); // Write the current counter value to the log file.
        }

        // This method is called when Loupedeck needs to show the command on the console or the UI.
        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            $"";

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {
                bitmapBuilder.SetBackgroundImage(PluginResources.ReadImage("D4.png"));
                bitmapBuilder.DrawText($"D{value}{Environment.NewLine}{this.hand}");

                return bitmapBuilder.ToImage();
            }
        }

    }
}