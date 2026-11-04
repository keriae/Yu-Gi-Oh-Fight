using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;

namespace MDPro3
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
            _renderer.material.shader = await MaterialLoader.LoadShaderByNameAsync(shaderName);
            _renderer.enabled = true;
        }
    }
}