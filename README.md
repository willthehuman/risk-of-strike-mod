# Risk Of Vampire

This is mod for Risk of Rain2([Steam](https://store.steampowered.com/app/632360/Risk_of_Rain_2/))

日本語は英語のあとに続きます。(See below for the Japanese version.)

## Update
Removed Scrappers BeGone and Ephemeral_Coins from dependencies. Please delete it by yourself.

依存関係からScrappersBeGoneとEphemeral_Coinsを取り除きました。各自で削除してください。

## English
A mod inspired by Vampire Survivors[(Steam)](https://store.steampowered.com/app/1794680/Vampire_Survivors/). With a number of changes, we aim to make the game a little more strategic.

When you open the chest, the item selection orb will appear. Two item choices will be drawn from {possessed items + one random item}, and you can choose from those two.
By collecting the items you want in the first stage, those items will be given priority in subsequent chests. It will lead to success that select items strategically.

From Ver2.0, "Limited item slot" system is introduced. When the item slot limit is reached, only the possessed items will come out from the chests. Fill the slots with only the items you want.

To reduce the boredom of the early stages of Monsoon, we are increasing the number of early enemy spawns.<br>
It has increased the amount of coins needed to open the chest. Besides that, we have made some difficulty adjustments.

I am adding items to the settings and changing the default values. If you update this mod, please check the settings and try default values.

<img src = "https://user-images.githubusercontent.com/5510944/157956620-aec42b86-b075-401a-a1d1-3f6002738004.png" width = '50%'>
<img src = "https://user-images.githubusercontent.com/5510944/157956105-ce9e2cd7-5c95-4690-9254-806d1c274c7f.png" width = '50%'>

If you have any problems or want to make adjustments, please contact [GitHub](https://github.com/motonari728/RoR2_Destiny_Mod) or Risk of Rain2 modding Discord(https://discord.gg/pW97gtA7hC). My discord tag is mochi#9204.

### Limited item slot system (Ver2.0.0 or higher)
The item slot limit has been introduced with reference to Vampire Survivors. By default, the slot limit for white item types is 5, and the slot limit for green item is 3. Game chat will display the count of owned items and slot limits.

After filling the limited slot, only the items you have will come out from the chest. Fill the slots with only the items you want.
<img src='https://user-images.githubusercontent.com/5510944/159090734-890a96ed-6c8d-4963-96e9-09aef089a969.png' width=50%>

### Probability of adding possessed items to the item picker
- White item: 100%
- Green item: 20%
- Red item: 5%
- Boss item: 5%
- Lunar item: 0%
- Corrupt(Void) item: See probability of original item

### Difficulty adjustment
It's easy because you can choose the item. So I added 3 more difficult difficulty harder than Monsoon.
- Difficulty 400%
- Difficulty 450%
- Difficulty 500%

### Configurable changes
- OSP Thureshold: One Shot Protection is set to 80% (adjustable), so you won't take more than 80% damage.
- Invulnerable Time: After OSP is activated, you are invincible for 0.5second (adjustable).
- Max Heal per Second: Heal is limited to 10% (adjustable) of total HP per second. Carryover is up to 200%. Mainly engineer's fungus countermeasures and prevent to play that recovers instantly.
- Money Scaling: Scaling the price to open a chest.
- Possessed Item Chance: The probability that your item will be added to the Item Picker lottery candidates. The higher it is, the easier it is for your items to appear as candidates.
- The spawn rate of Scrapper, MultiShop, 3D Printer, and Altar of Luck can now be adjusted. It is also possible to set it does not appear.
- The upper limit of the item slot. It can be set for each white item and green item.
- You can reload Config with F2 key.

### Other changes
- An item selection orb appears instead of an item from the box
- Item selection orb options is selected from the items you have. Game make choice at the moment you open the orb. The options are determined by the item of the person who opened it.
- Remove scrapper.
- The amount of HP increase for each Lv of the character is increased by 1.5 times. When the level goes up, HP will reach about (Original * 1.5).
- 1.5 times the number of monster spawns on difficulty LV 1-9
- The number of monster spawns is 1.25 times on difficulty levels Lv 10-15.
- Scaling the amount of money needed to open the box has increased significantly from 1.25 to 1.45

### Multiplay
It is available. In multiplayer, we are developing with the assumption that everyone will include this mod. Please install this mod in host and clients.
We have confirmed that people with this mod can multiplay without any problems. 

---------------------------------------
## 日本語
Vampire Survivors[(Steam)](https://store.steampowered.com/app/1794680/Vampire_Survivors/)にインスパイアされたModです。多数の変更により、もう少し戦略性の高いゲームに変えることを目標としています。

チェストを開けるとアイテム選択オーブが出てきます。アイテムの選択肢は{すでに持っているアイテム+ランダムアイテム１つ}から2つ抽選され、その２つから選ぶことが出来ます。最初のステージで欲しいアイテムを集めることで、以降のチェストからはそのアイテムが優先的に出てきます。戦略性を持ってアイテムの取捨選択をすることで攻略につながるでしょう。

Ver2.0からはアイテム枠上限制が導入され、アイテム枠上限に達した場合は所持アイテムのみしか出てこなくなります。欲しいアイテムのみで枠を埋めましょう。

Monsoonでの序盤の退屈さを軽減するために、序盤の敵のスポーン数を増やしています。
チェストを開けるのに必要なコインの量を、かなり増やしています。それ以外にも、いくつか難易度調整を行っています。

初期バージョンと比べて、設定に項目を追加したりデフォルト値を変更したりしています。アップデートした場合、設定とデフォルト値の確認をお願いします。

<img src="https://user-images.githubusercontent.com/5510944/157956620-aec42b86-b075-401a-a1d1-3f6002738004.png" width='50%'>
<img src="https://user-images.githubusercontent.com/5510944/157956105-ce9e2cd7-5c95-4690-9254-806d1c274c7f.png" width='50%'>

なにか問題がある場合や、調整が欲しい場合は[GitHub](https://github.com/motonari728/RoR2_Destiny_Mod)かRisk_of_Rain2(JP) Discord(https://discord.gg/jTbthYJ) までお願いします。開発者のDiscord Tagはmochi#9204です。

### アイテム上限の追加(Ver2.0.0以上)
Vampire Survivorsを参考にアイテムの枠上限が導入されました。デフォルトで白アイテム枠の上限が5, 緑アイテム枠の上限が3です。チャットに保持アイテムと枠の上限が表示されます。

枠を埋めたあとは、チェストから持っているアイテムしか出てきません。欲しいアイテムのみで枠を埋めましょう。
<img src='https://user-images.githubusercontent.com/5510944/159090734-890a96ed-6c8d-4963-96e9-09aef089a969.png' width=50%>


### アイテムピッカーへの所持アイテムの追加確率
- White item: 100%
- Green item: 20%
- Red item: 5%
- Boss item: 5%
- Lunar item: 0%
- Corrupt(Void) item: 元のアイテムの確率を参照

### 難易度調節
アイテムが選べるので簡単になります。そこでMonsoonよりさらに難しい難易度を３つ追加しました。
- 難易度 400%
- 難易度 450%
- 難易度 500%

### 設定可能な変更
- OSP Thureshold: One Shot Protectionを80%(調整可)にしてあるので、80%以上のダメージを食らうことがありません。
- Invulnerable Time: OSP発動後は、0.5秒(調整可)無敵です。
- Max Heal per Second: Healは秒間総HPの10%(調整可)が上限にしています。持ち越しは200%までです。主にエンジニアのきのこ対策と瞬時に回復するプレイを防ぐためです。
- Money Scaling: チェストを開ける値段のスケーリング。
- Possessed Item Chance: 所持アイテムがItem Pickerの抽選候補に加えられる確率。高くするほど、所持アイテムが候補に出やすくなります。
- Scrapper, MultiShop, 3D Printer, 運の祭壇の出現率が調整可能になりました。出現しなくなる設定も可能です。
- アイテム枠の上限。白アイテムと緑アイテムそれぞれに設定可能です。
- F2キーでConfigを再読み込みできます。

### その他の変更
- 箱からアイテムの代わりにアイテム選択オーブが出現
- アイテム選択オーブの選択肢が、手持ちのアイテムから選ばれるように変更。アイテム選択オーブを開けた瞬間に、開けた人のアイテムによって中身が決まります。
- スクラッパーの消去。
- キャラクターのLvごとのHP上昇量を1.5倍。レベルが上がりきったとき、HPは約1.5倍になります
- 難易度LV 1~9でモンスターのスポーン数1.5倍
- 難易度Lv10~15でモンスターのスポーン数1.25倍
- 箱を開けるのに必要なお金の量のスケーリングを1.25から1.45へかなり上昇

### マルチプレイ
利用可能です。マルチプレイでは、全員がこのModを入れることを想定して開発しています。全員Modを入れてご利用ください。
Modが入った人同士で問題なく動くことを確認しています。

## Changelog
**2.0.0**
- Limited item slot system is introduced.

**1.0.9**
- Configs are now synced during multiplayer. Always refer to the host's config.
- Green and red items are now guaranteed when you open the large and legendary chests.
- The spawn rate of Scrapper, MultiShop, 3D Printer, and Altar of Luck can now be adjusted.

**1.0.8**
- Update Readme and config description.

**1.0.6**
- Used items(Item tagged with No Tier) have been excluded from the lottery candidates.

**1.0.5**
- Add Ephemeral Coins mod as dependency mod for game balance.

**1.0.2**
- The probability that a boss item will be added to item picker has been reduced to 1/10.
- Lunar items are no longer added to candidates.

**1.0.1**
- Update Readme

**1.0.0**
- First Release.
