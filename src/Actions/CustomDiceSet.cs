namespace Loupedeck.DicePlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class CustomDiceSet : PluginDynamicAdjustment
    {
        private Int32 diceSides = 0;
        public CustomDiceSet()
    : base(displayName: "Set dice", description: "Set the value of the custom dice.", groupName: "Custom", hasReset: true)
        {
        }
        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            this.diceSides += diff;
            this.AdjustmentValueChanged();
            CustomDiceVar.setDiceSide(this.diceSides);
        }
        protected override void RunCommand(String actionParameter)
        {
            this.diceSides = 0;
            this.AdjustmentValueChanged();
            CustomDiceVar.setDiceSide(this.diceSides);
        }
        protected override String GetAdjustmentValue(String actionParameter) => this.diceSides.ToString();
    }
}
