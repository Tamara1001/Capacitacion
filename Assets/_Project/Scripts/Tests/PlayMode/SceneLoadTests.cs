using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LotoSim.Tests
{
    public class SceneLoadTests
    {
        [UnityTest]
        public IEnumerator BuildScenes_Load_HaveMainCamera([Values("Menu", "PumpRoom")] string sceneName)
        {
            yield return SceneManager.LoadSceneAsync(sceneName);
            Assert.IsNotNull(Camera.main, $"La escena {sceneName} no tiene Main Camera");
        }
    }
}
