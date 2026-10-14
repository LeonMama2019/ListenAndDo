# Stage5 ランダム出題の設定

既存の Stage5Data アセット（Stage5TaskData）をそのまま使います。
以前の固定問題と音声は「以前の固定問題」に残しています。
Objects と Question Formats が両方空なら従来どおり再生します。
どちらかに登録したら新形式を使い、不足設定はエラーとして表示します。

## 1. Objects（オブジェクト）
- Id：対象名。例 Pencil。同じ名前で Count 1、Count 2 の登録ができます。
- Level、Place：出題レベルと収納先。
- Sprite：対象の画像。本は BookFront の画像。
- Count：机に残す個数。
- Voice：名前を読む音声。複数個は「鉛筆2本」など個数まで入れた音声。
  後ろに続く「を」は質問形式側に登録するとつなぎやすくなります。

1つの問題では同じSpriteの数量違いを同時に選びません。
存在しない画像や実際の個数を超える指定はゲーム開始時に検出します。

## 2. Question Formats（質問形式）
- Id：形式の識別名。
- Level：0は全レベル共通、1〜8は特定レベル。
- Place：対応する収納先。
- Object Ids：空なら同じレベル・収納先の全対象。
  塗り絵用は Colorpencil、Crayon のように対応するIdだけを登録。
- Parts：再生順に追加します。
  - Audio：固定の音声を Voice に入れます。Text は記録用で画面には出しません。
  - Objects：今回抽選した対象をここで読み上げます。Voiceは不要です。

例A：Objects → Audio「を出して、その他は片付けて」
例B：Audio「皆で塗り絵をしましょう」→ Objects → Audio「を出して」
例C：Objects → Audio「以外はしまってね」

## 3. Conjunction Voice（接続音声）
「と」の音声を入れます。複数種類の場合だけ、名前の間に自動で入ります。

## 出題設定
Questions Per Level：1回の問題数（初期値7）。
Min / Max Object Kinds：1問で抽選する対象の種類数。個数はObjectsのCountです。
候補の種類数が最小数に届かない形式は抽選しません。

抽選結果の画像・個数を実行時のDeskObjectsに反映し、その内容で正誤を判定します。
聞き返しでは同じ抽選結果・音声順を使います。
素材音声は自動生成しません。新形式の音声を登録後にランダム出題が有効になります。

現在のゲーム本体で対応済みの収納先はPencaseとBookStandです。
Odouguの操作・重なり記録は別途実装が必要です。
