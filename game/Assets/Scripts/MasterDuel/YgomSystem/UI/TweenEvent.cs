namespace YgomSystem.UI
{
	public class TweenEvent : Tween
	{
		protected override void OnSetValue(float par)
		{
            // 事件型 Tween：不修改任何属性，仅依赖基类的 onFinished 回调
            // （常用于在 prefab 中作为延迟/链式节点，通过 onFinished 驱动后续 Tween）
        }
	}
}
