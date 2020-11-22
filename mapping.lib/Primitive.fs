module Primitive

type Point = int * int

module Point =
  let distance (y1: int, x1: int) (y2: int, x2: int) =
    ((pown (y2 - y1) 2) + (pown (x2 - x1) 2))
    |> float32
    |> sqrt

type Rect = Point * Point

let add (lhs: Point) (rhs: Point) =
  let ly, lx = lhs
  let ry, rx = rhs
  ly + ry, lx + rx

let sub (lhs: Point) (rhs: Point) =
  let ly, lx = lhs
  let ry, rx = rhs
  ly - ry, lx - rx

module Rect =
  let move (delta: int * int) (topLeft: int * int, bottomRight: int * int) =
    Rect(add topLeft delta, add bottomRight delta)

  let center (delta: int * int) ((top: int, left: int), (bottom: int, right: int)) =
    let disp = (bottom - top) / 2, (right - left) / 2
    Rect((disp |> sub (top, left) |> add delta), (disp |> sub (bottom, right) |> add delta))
