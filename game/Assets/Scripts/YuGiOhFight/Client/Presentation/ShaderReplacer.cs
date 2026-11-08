using Cysharp.Threading.Tasks;

using UnityEngine;

namespace YuGiOhFight.Client.Presentation
{
    [RequireComponent(typeof(Renderer))]
    public class ShaderReplacer : MonoBehaviour
    {
        public string shaderName;

        private Renderer _renderer;

        private Coroutine coroutine;

        private void Awake()
        {
            ReplaceShader();
        }

        private void ReplaceShader()
        {
            if(_renderer == null)
                _renderer = GetComponent<Renderer>();
            if(_renderer == null || string.IsNullOrEmpty(shaderName))
                return;

            _ = ReplaceShaderAsync();
        }

        private async UniTask ReplaceShaderAsync()
        {
            _renderer.enabled = false;
            _renderer.material.shader = await Client.UI.MaterialLoader.LoadShaderByNameAsync(shaderName);
            _renderer.enabled = true;
        }
    }
}