using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LightningAnimation;

namespace LightningAnimation.Tests
{
    public class BasicAnimationTests
    {
        private GameObject testObject;
        private PlayableAnimationController controller;
        private AnimationClip testClip;
        
        [SetUp]
        public void Setup()
        {
            testObject = new GameObject("TestAnimationObject");
            testObject.AddComponent<Animator>();
            controller = testObject.AddComponent<PlayableAnimationController>();
            
            // Create a simple test animation clip
            testClip = new AnimationClip();
            testClip.name = "TestAnimation";
            
            // Add a simple position animation
            var curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            testClip.SetCurve("", typeof(Transform), "localPosition.x", curve);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }
        
        [Test]
        public void Controller_InitializesCorrectly()
        {
            Assert.IsNotNull(controller);
            Assert.AreEqual(0, controller.CachedAnimationCount);
            Assert.IsFalse(controller.IsPlaying());
        }
        
        [Test]
        public void AddAnimation_WorksCorrectly()
        {
            controller.AddAnimation("TestAnim", testClip);
            
            Assert.AreEqual(1, controller.CachedAnimationCount);
            Assert.IsTrue(controller.HasAnimation("TestAnim"));
        }
        
        [Test]
        public void PlayAnimation_StartsCorrectly()
        {
            controller.AddAnimation("TestAnim", testClip);
            controller.Play("TestAnim");
            
            Assert.IsTrue(controller.IsPlaying());
            Assert.IsTrue(controller.IsPlaying("TestAnim"));
            Assert.AreEqual("TestAnim", controller.GetCurrentAnimation());
        }
        
        [UnityTest]
        public IEnumerator PlayAnimation_CallsCallback()
        {
            bool callbackCalled = false;
            
            controller.AddAnimation("TestAnim", testClip);
            controller.Play("TestAnim", () => callbackCalled = true);
            
            // Wait for animation to complete (testClip is 1 second long)
            yield return new WaitForSeconds(1.1f);
            
            Assert.IsTrue(callbackCalled);
        }
        
        [Test]
        public void StopAnimation_WorksCorrectly()
        {
            controller.AddAnimation("TestAnim", testClip);
            controller.Play("TestAnim");
            controller.Stop("TestAnim");
            
            Assert.IsFalse(controller.IsPlaying());
            Assert.IsFalse(controller.IsPlaying("TestAnim"));
        }
        
        [Test]
        public void ExtensionMethods_Work()
        {
            controller.AddAnimation("TestAnim", testClip);
            
            // Test GameObject extension
            testObject.PlayAnimation("TestAnim");
            Assert.IsTrue(controller.IsPlaying("TestAnim"));
            
            // Test stop extension
            testObject.StopAnimation("TestAnim");
            Assert.IsFalse(controller.IsPlaying("TestAnim"));
        }
    }
}