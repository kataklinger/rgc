module Filter

open System

let median (w: int) (img: Matrix.Matrix<byte>) =
  let w2 = w / 2

  let output =
    Matrix.Matrix<byte>(img.Width, img.Height)

  let getValue (arr: byte []) =
    let total = w * w

    let rec loop idx cnt =
      if cnt >= total / 2
      then idx
      else loop (idx + 1) (cnt + (int arr.[idx + 1]))

    byte <| loop 0 (int arr.[0])

  for outY in w2 .. img.Height - w2 - 1 do
    for outX in w2 .. img.Width - w2 - 1 do
      let counter = Array.zeroCreate 16
      for inY in outY - w2 .. outY + w2 do
        for inX in outX - w2 .. outX + w2 do
          let value = (int img.Rep.[inY, inX])
          counter.[value] <- (byte (counter.[value] + 1uy))
      output.Rep.[outY, outX] <- getValue counter
  output

let private gaussianKernel (dev: float32) (size: int) =
  let floatPi = float32 (Math.PI)
  let floatE = float32 (Math.E)

  let d = dev ** 2.f
  let a = 1.f / (2.f * floatPi * d)

  let fill y x =
    let dx, dy =
      float32 (x - size / 2), float32 (y - size / 2)

    let m = (dx ** 2.f + dy ** 2.f) / -(2.f * d)
    a * (floatE ** m)

  Matrix.Matrix<float32>(size, size, fill)

let gaussian (dev: float32) =
  let size = (int (ceil 6.f * dev)) ||| 1
  gaussianKernel dev size
