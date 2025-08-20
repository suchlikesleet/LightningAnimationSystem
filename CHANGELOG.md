# Changelog
All notable changes to the Lightning Animation System will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2024-01-15

### Added
- Initial release of Lightning Animation System
- High-performance animation playback using Unity's Playables API
- Support for multiple concurrent animations
- Animation crossfading and blending
- Animation queuing system
- Loop control with configurable loop count
- Speed control (per-animation and global)
- Pause/Resume functionality
- Event callbacks (OnStart, OnEnd, OnInterrupted, OnLoop)
- Extension methods for easy GameObject integration
- Custom Editor window for animation management
- Inspector enhancements with live preview
- Auto-optimization when idle
- Comprehensive debug logging

### Features
- **Performance Optimized**: Built on Unity's Playables API for minimal overhead
- **No Animator Controller Required**: Direct animation playback without state machines
- **Flexible Playback Modes**: Single, Additive, and Queue modes
- **Rich Event System**: Full lifecycle callbacks for animations
- **Editor Integration**: Custom inspector and editor window
- **Lightweight**: Minimal memory footprint and CPU usage

### Technical Details
- Minimum Unity Version: 2019.4 LTS
- No external dependencies
- Thread-safe animation management
- Automatic resource cleanup
- Support for up to 8 concurrent animations