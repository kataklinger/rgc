module Image

open Matrix

let rawToC64 (input: byte []) =
  Matrix
    (C64Consts.screenWidth,
     C64Consts.screenHeight,
     (fun y x -> input.[y * C64Consts.screenWidth + x]))

let c64ToRgb (input: Matrix<byte>) = input |> Matrix.map Palette.c64ToRgb

let imgc64ToRgbensity (input: Matrix<byte>) = input |> Matrix.map Palette.c64ToRgbInt

let c64ToGs (input: Matrix<byte>) = input |> Matrix.map Palette.c64ToGs

let rgbToInt (input: Matrix<int>) = input |> Matrix.map Palette.rgbToInt

let intToRgbGs (input: Matrix<float32>) = input |> Matrix.map Palette.intToRgbGs

let gsToC64 (input: Matrix<byte>) = input |> Matrix.map Palette.gsToC64

let gsToRgb (input: Matrix<byte>) = input |> Matrix.map Palette.gsToRgb

let gsToRgbGs (input: Matrix<byte>) = input |> Matrix.map Palette.gsToRgbGs
