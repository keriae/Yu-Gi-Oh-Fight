namespace YuGiOhFight.Client.UI
{
    public class ButtonListForOnline : ButtonList
    {
        public int id = 0;
        public override void SelectThis()
        {
            base.SelectThis();
            //Program.instance.online.SwitchFunction(id);
        }
    }
}
