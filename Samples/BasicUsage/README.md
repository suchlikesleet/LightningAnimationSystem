\# Lightning Animation System - Basic Usage Sample



This sample demonstrates the basic functionality of the Lightning Animation System.



\## What's Included



\- \*\*BasicUsageExample.cs\*\* - Main example script showing core features

\- \*\*BasicUsageExample.unity\*\* - Demo scene with character setup

\- \*\*Animation clips\*\* - Sample animations for testing



\## How to Use



1\. Open the `BasicUsageExample.unity` scene

2\. Press Play

3\. Use the following controls:

&nbsp;  - \*\*W\*\* - Walk animation

&nbsp;  - \*\*S\*\* - Idle animation  

&nbsp;  - \*\*A\*\* - Run animation

&nbsp;  - \*\*D\*\* - Jump animation

&nbsp;  - \*\*Shift (hold)\*\* - 2x speed

&nbsp;  - \*\*Ctrl (hold)\*\* - 0.5x speed



\## Key Features Demonstrated



\- Basic animation playback

\- Animation callbacks

\- Crossfading between animations

\- Speed control

\- Event handling

\- Extension methods



\## Code Highlights



```csharp

// Simple play

animController.Play("Walk");



// With callback

animController.Play("Jump", () => Debug.Log("Jump finished!"));



// With crossfade

animController.PlayWithCrossfade("Run", 0.3f);



// Extension method

gameObject.PlayAnimation("Idle");

