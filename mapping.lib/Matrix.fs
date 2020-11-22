module Matrix

type Matrix<'T>(width: int, height: int, init: int -> int -> 'T) =
  let rep = Array2D.init height width init

  new(width: int, height: int) = Matrix(width, height, (fun _ _ -> Unchecked.defaultof<'T>))

  new(value: 'T, width: int, height: int) = Matrix(width, height, (fun _ _ -> value))

  new(arr: 'T [,], width: int, height: int) = Matrix(width, height, (fun y x -> arr.[y, x]))

  member _.Rep = rep
  member _.Width = width
  member _.Height = height
  member _.Rect = Primitive.Rect((0, 0), (height, width))

let map (mapping: 'T -> 'U) (input: Matrix<'T>) =
  Matrix<'U>(input.Width, input.Height, (fun y x -> input.Rep.[y, x] |> mapping))

let frameInit (init: int -> int -> 'T)
              (top: int)
              (left: int)
              (bottom: int)
              (right: int)
              (input: Matrix<'T>)
              =
  if top = 0 && bottom = 0 && left = 0 && right = 0 then
    input
  else
    let height = input.Height
    let width = input.Width

    let readSrc y x =
      match y - top, x - left with
      | sy, sx when sx >= 0 && sx < width && sy >= 0 && sy < height -> input.Rep.[sy, sx]
      | _ -> init y x

    Matrix(width + left + right, height + top + bottom, readSrc)

let frame (def: 'T) = frameInit (fun _ _ -> def)

let frameZero (top: int) (left: int) (bottom: int) (right: int) (input: Matrix<'T>) =
  frame Unchecked.defaultof<'T> top left bottom right input

let copy (y: int) (x: int) (src: Matrix<'T>) (dst: Matrix<'T>) =
  for sy in 0 .. src.Height - 1 do
    for sx in 0 .. src.Width - 1 do
      dst.Rep.[y + sy, x + sx] <- src.Rep.[sy, sx]
  dst
