# ⚡ Lightning Animation System

A high-performance, lightweight animation framework for Unity built on the Playables API. Perfect for developers who need direct animation control without the overhead of Animator Controllers.

## 🚀 Features

- **High Performance**: Built on Unity's Playables API for minimal overhead
- **No Animator Controller Required**: Direct animation playback without state machines
- **Multiple Playback Modes**: Single, Additive, and Queue modes
- **Animation Blending**: Smooth crossfade transitions between animations
- **Loop Control**: Configure loop count or infinite looping
- **Speed Control**: Per-animation and global speed multipliers
- **Event System**: Comprehensive callbacks for animation lifecycle
- **Editor Integration**: Custom inspector and dedicated editor window
- **Auto-Optimization**: Automatically pauses graph when idle
- **Extension Methods**: Easy integration with GameObjects

## 📦 Installation

### Via Unity Package Manager
1. Open Package Manager (Window > Package Manager)
2. Click the '+' button and select "Add package from git URL"
3. Enter: `https://github.com/yourusername/lightning-animation-system.git`

### Manual Installation
1. Download the latest release
2. Extract to your project's `Assets` folder
3. The system is ready to use!

## 🎮 Quick Start

### Basic Usage

```csharp
using UnityEngine;
using LightningAnimation;

public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] private AnimationClip walkClip;
    [SerializeField] private AnimationClip runClip;
    [SerializeField] private AnimationClip jumpClip;
    
    private PlayableAnimationController animController;
    
    void Start()
    {
        // Get or add the animation controller
        animController = gameObject.GetOrAddAnimationController();
        
        // Add animations
        animController.AddAnimation("Walk", walkClip);
        animController.AddAnimation("Run", runClip);
        animController.AddAnimation("Jump", jumpClip);
        
        // Subscribe to events
        animController.OnAnimationEnd += OnAnimationComplete;
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            // Simple play
            animController.Play("Walk");
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            // Play with crossfade
            animController.PlayWithCrossfade("Run", 0.3f);
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            // Play with callback
            animController.Play("Jump", () => {
                Debug.Log("Jump finished!");
                animController.Play("Walk");
            });
        }
    }
    
    void OnAnimationComplete(string animationName)
    {
        Debug.Log($"Animation {animationName} completed!");
    }
}
```

### Using Extension Methods

```csharp
// Play animation directly on GameObject
gameObject.PlayAnimation("Walk");

// With crossfade
gameObject.PlayAnimationWithCrossfade("Run", 0.3f);

// Check if playing
if (gameObject.IsPlayingAnimation())
{
    // Animation is active
}

// Stop all animations
gameObject.StopAllAnimations();
```

### Advanced Features

```csharp
// Queue animations
animController.PlayWithMode("Attack", PlayMode.Queue);

// Loop animation 3 times
animController.PlayLooped("Dance", 3, () => Debug.Log("Dance finished!"));

// Infinite loop
animController.PlayLooped("Idle", -1);

// Speed control
animController.SetSpeed("Walk", 1.5f);  // 1.5x speed for Walk
animController.SetGlobalSpeed(2f);      // 2x speed for all

// Pause/Resume
animController.Pause("Walk");
animController.Resume("Walk");
animController.PauseAll();
animController.ResumeAll();
```

## 🎨 Editor Features

### Custom Inspector
- Live animation preview
- Runtime controls (Play, Pause, Stop)
- Speed adjustment sliders
- Animation queue visualization
- Performance metrics

### Editor Window
Access via `Window > Lightning Animation > Animation Controller`
- Global animation management
- Batch operations
- Performance profiling
- Animation testing tools

## 📊 Performance

The Lightning Animation System is optimized for performance:
- **Zero Allocation Updates**: No garbage generation during playback
- **Auto-Optimization**: Graph pauses when idle
- **Efficient Mixing**: Up to 8 concurrent animations
- **Direct Playables**: Bypasses Animator Controller overhead

### Benchmarks
- **Setup Time**: < 1ms for 10 animations
- **Per-Frame Cost**: ~0.05ms per active animation
- **Memory Usage**: ~2KB per cached animation

## 🔧 Configuration

### Component Settings
- **Auto Stop On Complete**: Automatically stop non-looping animations
- **Max Concurrent Animations**: Maximum simultaneous animations (1-8)
- **Auto Optimize When Idle**: Pause graph when no animations playing
- **Enable Debug Logging**: Show detailed logs in console

### Preload Animations
Add animations in the inspector's "Preload Animations" array to automatically load them at startup.

## 📚 API Reference

### Core Methods
- `Play(string name, Action onComplete = null)` - Play animation
- `PlayWithCrossfade(string name, float fadeTime, Action onComplete = null)` - Play with blend
- `PlayWithMode(string name, PlayMode mode, Action onComplete = null)` - Play with specific mode
- `PlayLooped(string name, int loops = -1, Action onComplete = null)` - Play with loop control
- `Stop(string name)` - Stop specific animation
- `StopAll()` - Stop all animations
- `Pause(string name)` / `PauseAll()` - Pause animations
- `Resume(string name)` / `ResumeAll()` - Resume animations

### Events
- `OnAnimationStart` - Fired when animation starts
- `OnAnimationEnd` - Fired when animation completes
- `OnAnimationInterrupted` - Fired when animation is interrupted
- `OnAnimationLoop` - Fired on each loop iteration

### Query Methods
- `IsPlaying()` / `IsPlaying(string name)` - Check if playing
- `GetCurrentAnimation()` - Get primary animation name
- `GetNormalizedTime(string name)` - Get progress (0-1)
- `GetLoopCount(string name)` - Get current loop iteration
- `HasAnimation(string name)` - Check if animation exists

## 🐛 Troubleshooting

### Animation Not Playing
- Ensure the GameObject has an Animator component
- Check that the animation clip is added to the controller
- Verify the animation name matches exactly (case-sensitive)

### Inspector Not Responsive
- Make sure you're using the latest version
- Try reimporting the package
- Check console for any error messages

### Performance Issues
- Reduce `Max Concurrent Animations` if not needed
- Enable `Auto Optimize When Idle`
- Use object pooling for frequently animated objects

## 📄 License

MIT License - See [LICENSE](LICENSE) file for details

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📮 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/lightning-animation-system/issues)
- **Documentation**: [Wiki](https://github.com/yourusername/lightning-animation-system/wiki)
- **Discord**: [Join our Discord](https://discord.gg/yourinvite)

## 🏆 Credits

Created by **GanKanStudio**

Special thanks to the Unity community for feedback and contributions.

---

**Lightning Animation System** - Fast, Lightweight, Powerful 
v1.0.0