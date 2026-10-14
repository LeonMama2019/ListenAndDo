# Stage5 ランダム出題設定

以前の固定問題と自動移行処理は削除しました。
既存のStage5Dataアセットを使い、新しい項目を登録してください。

## Objects：素材情報だけ
- Id：オブジェクト名（例：Pencil）
- Sprite：画像（本はBookFrontの画像）
- Voice：名前だけの音声
レベル・収納先・個数はここに登録しません。

## Levels：出題設定
- Level：レベル
- Place：収納先
- Question Count：1回の問題数
- Min / Max Object Kinds：1問で出す種類数
- Targets：出題候補
  - Object Id：Objectsに登録した名前
  - Count：机に残す個数
  - Quantity Voice：2個以上の時の個数込み音声（例「鉛筆2本」）
1個ならObjectsの名前の音声を使います。
同じ名前でCount 1とCount 2を登録すれば個数も抽選します。
Objects自体は1つの登録を複数レベルから参照できます。

## Question Formats：質問形式
Level 0は全レベル共通。
Object Idsが空ならそのレベルの全候補、指定したらその名前だけが対象です。
Partsは再生順に並べます。
- Audio：固定音声。Textは記録用で、画面には表示しません。
- Objects：今回選んだ対象の読み上げを差し込みます。

例：Objects → Audio「を出して、他はしまって」
例：Audio「皆で塗り絵をしましょう」→ Objects → Audio「を出して」

## Conjunction Voice
「と」の音声。複数種類の場合に名前の間で再生します。

選択した画像は個数分だけ実行時のDeskObjectsに入り、個数も含めて判定します。
聞き返しても抽選し直しません。
音声素材は自動生成されないため、登録してから実行してください。
お道具箱の操作・重なり記録の実装は、この設定構造変更とは別です。
