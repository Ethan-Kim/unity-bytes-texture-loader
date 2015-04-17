# unity-bytes-texture-loader

Impressed by [Demosthenes's article](http://www.gamedev.net/blog/591/entry-2260598-reducing-unity-game-file-size/)
Use this script to reduce unity build size.

### Features

* Sprite Texture Loader
* Sprite Animation Loader
* Simple Sprite Texture Pool
* Simple Sprite Animation Pool
* Animator Helper
* And Editor Inspector Helper

## Sprite Texture Loader Usage

How to use SpriteTextureLoader Step by Step

- Add Image or SpriteRenderer to GameObject
- Assign a texture you want.
- Add SpriteTextureLoader script.
- Select 'Type' you added(Image or SpriteRenderer).
- Please select another GameObject and select it. - There is a bug. SpriteTextureLoaderEditor coundn't refresh itself.
- Check options you want.
* ColorChangeWhenComplete : If sprite is loaded, they appears smoothly.
* AutomaticLoad : When this object enables, Load automatically.
* LoadInRuntime : Use this object with SpriteTexturePool.
- Click "MakeImageName"
* Image Name, Image Idx property will be changed.
- Click "Create Meta Data"
* This creates meta data about your texture import setting. Such as Single, Multiple, Pivots.
* File name will be /Assets/StreamingAssets/[Image Name].json
- Click "Create .bytes Texture"
* This function copies /Assets/foo/bar/[Image Name].* to /Assets/StreamingAssets/[Image Name].bytes so Unity recognize this as text.
- <b>Don't click "Clear" button.</b> This clears SpriteTextureLoader settings.
* Thie means you will assign Image name manually.
- Click "Finish".

## SpriteTextureLoaderEditor

This script creates some context menus in Project Window.
You can click "Create .bytes Texture" menu to create .bytes texture manually.

## Sprite Animation Loader Usage

This script works with SpriteTextureLoader to use Animator.
With this script, You can implement sprite animation.

- You should create Animator and Animations.
- Add SpriteTextureLoader script and create .bytes texture described in Sprite Texture Loader Usage.
- Add SpriteAnimationLoader script.
- Check options you want.
* ColorChangeWhenComplete : If sprite and animation are loaded, they appears smoothly.
* AutomaticLoad : When this object enables, Load automatically.
* LoadInRuntime : Use this object with SpriteTexturePool.
* AutoEnable : When this object enables, Play automatically.
- Before Use this Click "Make First Frame" in Animator.
* This described in below AnimatorEditor section.
- Click "Make AnimatorName".
* Animator Name property will be changed.
- Click "Create Animator Meta Data".
* This creates meta data about your animator in /Assets/StreamingAssets/Anim_[Animator Name].json
- Click "Remove Sprite Animator Data".
* This function will remove your animator data so that their sprite animation data will be deleted.
- <b>Don't Click "Restore Sprite Animator Data".</b>
* This needs when you edit animation data in Animator and Animation Window.
- <b>Don't Click "Clear". </b>
* Thie means you will assign Animator name manually.
- Click "Finish".

## SpriteAnimationLoaderEditor

Nothing to explain.

## SpriteTexturePool

This script implements simple texture pool.
Supports Preload enumerator to load all textures when start. This prevents loading lag.

- Add GameObject into your first scene.
- Add SpriteTexturePool script.
- If you want to load all textures in loading time. Use Preload Enumerator.

## SpriteTexturePoolEditor

- Click "Make Streaming Texture List" to save all .bytes texture lost.
- Save this GameObject.

## SpriteAnimationPool

This script implements simple animator pool.
Supports Preload enumerator to load all animators when starts. This prevents loading lag.

- Add GameObject into your first scene.
- Add SpriteAnimationPool script.
- If you want to load all textures in loading time. Use Preload Enumerator.

## SpriteAnimationPoolEditor

- Click "Make Streaming Animator List" to save all .bytes texture lost.
- Save this GameObject.

## AnimatorEditor

If your game change animatior transition. SpriteAnimationLoader coundn't know about first frame's image or sprite assignment.
So you should make meta data about it.
This creates "Make First Frame" button in Animator to do this.

## Some code snippets

Change Animator in runtime.
```csharp
var animLoader = Cat.GetComponent<SpriteAnimationLoader> ();
animLoader.ChangeTo ("newAnimatorName");
animLoader.Load ();
```

Change Sprite in runtime.
```csharp
var loader = image.GetComponent <SpriteTextureLoader> ();
loader.ChangeTo ("newImageName", "newImageIndex");
loader.Load ();
```

## Applied Project

I use these script to development my game.
It's on Apple Appstore and Google Playstore.

[Apple Appstore Paid](http://appstore.com/findingsally)

[Google Playstore Free](https://play.google.com/store/apps/details?id=com.toripacktory.findingbutlerlite)

[Google Playstore Paid](https://play.google.com/store/apps/details?id=com.toripacktory.findingbutler)
