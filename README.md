# VRM_Rec_AB

VRM_Rec_ABは、Controlnet学習用データセット作成アプリです。

## ダウンロード

### アプリケーション

ビルド済みのアプリケーションは以下のリンクからダウンロードできます：

- [Windows版](https://github.com/tori29umai0123/VRM_Rec_AB/releases/download/VRM_Rec_AB_V1/VRM_Rec_AB_win.zip)
- [Linux版](https://github.com/tori29umai0123/VRM_Rec_AB/releases/download/VRM_Rec_AB_V1/VRM_Rec_AB_linux.zip)

### VRMファイル

テスト用のVRMファイルは以下のリンクからダウンロードできます：

- [テスト用VRMファイル](https://drive.google.com/file/d/1b_GtytwMdASTpzZjibxrLCLHiZgikRmu/view?usp=sharing)

## 使い方

1. 上記のリンクから、アプリケーションとVRMファイルをダウンロードします。

2. 以下のUnityパッケージをインポートします：
   - [UniVRM v0.126.0](https://github.com/vrm-c/UniVRM/releases/download/v0.126.0/VRM-0.126.0_14f3.unitypackage)
   - [ポーズ詰め合わせ（有料版）](https://booth.pm/ja/items/1634088)

3. ポーズ詰め合わせ（有料版）から以下のディレクトリ内のanimファイルを `VRM_Rec_AB\Assets\Resources` 直下に全てコピーします：

   ```
   VRM_Rec_AB\Assets\Necocoya\ポーズ詰め合わせ_有料版_v130\Pose030_日常
   VRM_Rec_AB\Assets\Necocoya\ポーズ詰め合わせ_有料版_v130\Pose040_ネタ系
   VRM_Rec_AB\Assets\Necocoya\ポーズ詰め合わせ_有料版_v130\Pose050_ジャンプ系
   VRM_Rec_AB\Assets\Necocoya\ポーズ詰め合わせ_有料版_v130\Pose060_モブ系
   VRM_Rec_AB\Assets\Necocoya\ポーズ詰め合わせ_有料版_v130\Pose070_せくしぃ系
   VRM_Rec_AB\Assets\Necocoya\ポーズ詰め合わせ_有料版_v130\Pose010_女性_立ち
   VRM_Rec_AB\Assets\Necocoya\ポーズ詰め合わせ_有料版_v130\Pose011_女性_座り
   VRM_Rec_AB\Assets\Necocoya\ポーズ詰め合わせ_有料版_v130\Pose012_女性_その他
   VRM_Rec_AB\Assets\Necocoya\ポーズ詰め合わせ_有料版_v130\Pose020_男性
   ```

4. `VRM_Rec_AB\Assets\Resources\config.ini` ファイルを設定します。

5. 設定完了後、アプリケーションを実行すると撮影が開始されます。

## ビルド版の使用方法

ビルド版をコンソールから実行することもできます。

### Windows:
```
./VRM_Rec_AB.exe -batchmode -logFile log.txt
```
注意: このコマンドではコンソール出力は表示されませんが、ログファイルが生成されます。

### Linux:
```
./VRM_Rec_AB.x86_64 --headless
```
このコマンドではコンソールに出力が表示されます。

## 注意事項

- VRMファイルとポーズ詰め合わせ（有料版）は別途入手する必要があります。
- 撮影前に必ず `config.ini` の設定を確認してください。
- ディレクトリパスは環境に応じて適切に変更してください。

## 設定パラメータの説明

`config.ini` ファイルで設定可能なパラメータの詳細説明：

1. blendShapeNames
   - VRMモデルに適用する表情（ブレンドシェイプ）のリストです。
   - カンマ区切りで複数の表情を指定でき、ランダムに選択されます。

2. output_A, output_B
   - 撮影した写真の保存先ディレクトリです。
   - A, Bはそれぞれ異なるVRMモデルの写真用です。

3. VRMDirA, VRMDirB
   - VRMファイルが格納されているディレクトリのパスです。
   - AとBで異なるモデルセットを使用できます。

4. NeckBoneName, UpperChestBoneName
   - カメラのフォーカス対象となるボーンの名前です。
   - 上半身と全身の撮影時に使用されます。

5. startPhotoNumber
   - 撮影を開始する写真の番号です。
   - 既存の写真がある場合、この番号から続けて撮影できます。

6. overwriteExistingFiles
   - 既存の写真ファイルを上書きするかどうかを指定します。
   - falseの場合、既存のファイルはスキップされます。

7. disableSilhouetteMode
   - シルエットモードを無効にするかどうかを指定します。
   - falseの場合、シルエット撮影も行われます。

8. waitTime
   - ポーズ適用後、撮影までの待機時間（秒）です。

9. shots
   - 撮影する写真の総数です。

10. radius_upperbody_min, radius_upperbody_max
    - 上半身撮影時のカメラ距離の最小値と最大値（単位：メートル）です。

11. radius_body_min, radius_body_max
    - 全身撮影時のカメラ距離の最小値と最大値（単位：メートル）です。

これらのパラメータを調整することで、VRMモデルの撮影設定をカスタマイズできます。例えば、カメラ距離や撮影枚数を変更したり、特定の表情のみを使用したりすることが可能です。また、出力ディレクトリやVRMファイルの場所も柔軟に設定できます。