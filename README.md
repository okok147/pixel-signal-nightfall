# Pixel Signal: Nightfall — Unity 2D

一個沒有第三方素材依賴的原創 survivor-like 2D pixel-art game。角色、敵群、投射物、經驗碎片、寶箱與 HUD 在執行時生成，因此專案很輕，適合先在 Unity 裡快速迭代。

## Nightfall Meadow redesign

`agent/cute-nightfall-redesign` 分支加入完整的原創柔和奇幻展示層：

- 草地、花、石頭、螢火與月光構成的夜間場景；
- 矮身可愛的 Meadow Courier、Dusk Slime、Moonhorn；
- 木框、羊皮紙、暖色與緊湊 HUD；
- 底部裝備列與全寬 XP bar；
- 三張大型升級卡與更友善的文字；
- 可重用、無外部依賴的程式生成像素素材工具。

`CuteNightfallPresentation.cs` 會保留現有 `PixelSurvivorGame` 戰鬥模擬，停用舊開發者 HUD，再套用新場景、角色和 UI。這讓目前版本仍可遊玩，同時為下一階段拆分正式系統留出空間。

美術與 UX 規則見 [`ART_DIRECTION.md`](ART_DIRECTION.md)。下次開發可直接使用 [`FUTURE_DEVELOPMENT_PROMPT.md`](FUTURE_DEVELOPMENT_PROMPT.md)。

整體方向採用原創的柔和凱爾特童話與 fantasy-life 氣氛；不使用或抽取《瑪奇》、Vampire Survivors 或其他遊戲的角色、素材、介面、圖示或商標。

## 開啟

1. 將此 repository clone 後，使用 Unity Hub 開啟 repository 根目錄。
2. 使用 Unity 6.5 `6000.5.6f1`。
3. 開啟 `Assets/Scenes/Main.unity`，按 Play。

第一次匯入時，`Assets/Editor/SignalDriftSceneSetup.cs` 會自動建立 Main Camera 與場景。

## 操作

- `Enter` / `Space`：開始
- `WASD` / 方向鍵：移動
- `Space`：脈衝，清除附近敵人（遊戲開始後）
- `1` / `2` / `3` 或滑鼠：選擇升級
- `P`：暫停／繼續
- `R`：勝利或失敗後重新開始
- `F`：切換全螢幕

目標是在 180 秒內建立自己的自動攻擊 loadout 並活過敵群。擊敗敵人會掉落 XP；升級時會暫停並提供 3 張卡；`Spark Wand + Ember Ring + Wand Lv.3` 可進化成 `Cinder Volley`。

遊戲設計與玩法規格見 [`GAME_DESIGN.md`](GAME_DESIGN.md)。
