using System.Runtime.CompilerServices;


namespace YuGiOhFight.Client.Presentation.UI.ElementWidget
{
	public class InputFieldWrapper
	{
		private enum Mode
		{
			uGUI = 0,
			TMP = 1
		}

		public class OnChangedEvent : UnityEngine.Events.UnityEvent<string>
		{
			public OnChangedEvent()
			{
				//((UnityEvent<T0>)(object)this)._002Ector();
			}
		}

		private Mode mode;

		private ExtendedInputField inputField;

		private TMPro.TMP_InputField TMPInputField;

		public TextWrapper textComponent
		{
			[CompilerGenerated]
			get
			{
				return null;
			}
			[CompilerGenerated]
			private set
			{
			}
		}

		private UnityEngine.UI.Selectable selectable => null;

		public bool interactable
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public string text
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public UnityEngine.GameObject gameObject => null;

		public UnityEngine.UI.Graphic targetGraphic => null;

		public UnityEngine.Transform transform => null;

		public OnChangedEvent onValueChanged
		{
			[CompilerGenerated]
			get
			{
				return null;
			}
			[CompilerGenerated]
			set
			{
			}
		}

		public UnityEngine.UI.InputField.ContentType contentType
		{
			get
			{
				return default(UnityEngine.UI.InputField.ContentType);
			}
			set
			{
			}
		}

		public int characterLimit
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public UnityEngine.UI.Graphic placeholder => null;

		public int caretPosition
		{
			get
			{
				return 0;
			}
			set
			{
			}
		}

		public InputFieldWrapper(ExtendedInputField inputField)
		{
		}

		public InputFieldWrapper(TMPro.TMP_InputField inputField)
		{
		}

		public void InvokeOnValueChanged(string text)
		{
		}

		public void SetTextWithoutNotify(string text)
		{
		}

		public void ActivateInputField()
		{
		}

		public void DeactivateInputField()
		{
		}
	}
}
