module Palette

let private c64Palette =
  [| 0x00000000
     0x00FFFFFF
     0x0068372B
     0x0070A4B2
     0x006F3D86
     0x00588D43
     0x00352879
     0x00B8C76F
     0x006F4F25
     0x00433900
     0x009A6759
     0x00444444
     0x006C6C6C
     0x009AD284
     0x006C5EB5
     0x00959595 |]

let decodeRgb (value: int) =
  (byte value), (byte (value >>> 8)), (byte (value >>> 16))

let encodeRgb (r: byte, g: byte, b: byte) =
  (int r) ||| ((int g) <<< 8) ||| ((int b) <<< 16)

let rgbDecodedToInt (r: byte, g: byte, b: byte) =
  (0.3f
   * (float32 r)
   + 0.59f * (float32 g)
   + 0.11f * (float32 b))
  / 255.0f

let rgbToInt (value: int) = value |> decodeRgb |> rgbDecodedToInt

let c64ToRgbInt (value: byte) = c64Palette.[(int32 value)] |> rgbToInt

let intToRgbDecoded (value: float32) =
  (byte (255.0f * value)), (byte (255.0f * value)), (byte (255.0f * value))

let intToRgbGs (value: float32) = value |> intToRgbDecoded |> encodeRgb

let private gsPalette =
  c64Palette
  |> Array.mapi (fun idx value -> (idx, rgbToInt value))
  |> Array.sortBy snd
  |> Array.mapi (fun idx (value, _) -> (idx, value))
  |> Array.sortBy snd
  |> Array.map (fst >> byte)

let c64ToRgb (value: byte) = c64Palette.[(int32 value)]

let c64ToGs (value: byte) = gsPalette.[(int32 value)]

let gsReversePallet =
  gsPalette
  |> Array.mapi (fun idx value -> (idx, value))
  |> Array.sortBy snd
  |> Array.map (fst >> byte)

let gsToC64 (value: byte) = gsReversePallet.[(int32 value)]

let gsToRgb (value: byte) = value |> gsToC64 |> c64ToRgb

let gsToInt (value: byte) = value |> gsToC64 |> c64ToRgbInt

let gsToRgbGs (value: byte) =
  value |> gsToC64 |> c64ToRgbInt |> intToRgbGs
