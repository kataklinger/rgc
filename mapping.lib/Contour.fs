module Contour

open System
open System.Security.Cryptography

type Contour =
  { key: int
    color: byte
    area: int
    region: Primitive.Rect
    start: (int * int)
    edges: uint16 [] }

type Buffers =
  { keys: Matrix.Matrix<int>
    edges: Matrix.Matrix<int>
    walk: (int * int) [] }

let buffer (img: Matrix.Matrix<byte>) =
  let buf =
    { keys = Matrix.Matrix<int>(img.Width, img.Height)
      edges = Matrix.Matrix<int>(img.Width, img.Height)
      walk = Array.init (img.Height * img.Width) (fun _ -> 0, 0) }

  for y in [| 0; img.Height - 1 |] do
    for x in 0 .. img.Width - 1 do
      img.Rep.[y, x] <- 32uy
      buf.keys.Rep.[y, x] <- -1

  for x in [| 0; img.Width - 1 |] do
    for y in 0 .. img.Height - 1 do
      img.Rep.[y, x] <- 32uy
      buf.keys.Rep.[y, x] <- -1

  buf

let single (buf: Buffers) (y: int, x: int) (key: int) (img: Matrix.Matrix<byte>) =
  let color = img.Rep.[y, x]

  let mutable area = 1

  let edges =
    new System.Collections.Generic.List<int>()

  let mutable walkTail = 1
  let mutable walkHead = 0
  buf.walk.[walkHead] <- (y, x)

  buf.keys.Rep.[y, x] <- key

  let mutable minY = y
  let mutable minX = x
  let mutable maxY = y
  let mutable maxX = x

  while walkHead < walkTail do
    let (yWalk, xWalk) = buf.walk.[walkHead]
    walkHead <- walkHead + 1

    let mutable isEdge = false
    for inY in yWalk - 1 .. 2 .. yWalk + 1 do
      if img.Rep.[inY, xWalk] = color then
        if (buf.keys.Rep.[inY, xWalk] = 0) then
          buf.keys.Rep.[inY, xWalk] <- key
          buf.walk.[walkTail] <- inY, xWalk
          walkTail <- walkTail + 1
          area <- area + 1
      else
        buf.edges.Rep.[yWalk, xWalk] <- key
        isEdge <- true
    for inX in xWalk - 1 .. 2 .. xWalk + 1 do
      if img.Rep.[yWalk, inX] = color then
        if (buf.keys.Rep.[yWalk, inX] = 0) then
          buf.keys.Rep.[yWalk, inX] <- key
          buf.walk.[walkTail] <- yWalk, inX
          walkTail <- walkTail + 1
          area <- area + 1
      else
        buf.edges.Rep.[yWalk, xWalk] <- key
        isEdge <- true

    if isEdge then edges.Add((yWalk <<< 16) ||| xWalk)
    if yWalk > maxY then maxY <- yWalk
    else if yWalk < minY then minY <- yWalk

    if xWalk > maxX then maxX <- xWalk
    else if xWalk < minX then minX <- xWalk

  edges.Sort()
  let mutable lastLine = 0us

  let encoded =
    new System.Collections.Generic.List<uint16>()

  for n in 0 .. edges.Count - 1 do
    let e = edges.[n]
    let y, x = uint16 (e >>> 16), uint16 (e &&& 0xffff)
    if lastLine <> y then
      encoded.Add(0x8000us ||| y)
      lastLine <- y
    encoded.Add(x)

  { key = key
    color = color
    area = area
    region = (minY, minX), (maxY, maxX)
    start = edges.[0] >>> 16, edges.[0] &&& 0xffff
    edges = encoded.ToArray() }

let extract (img: Matrix.Matrix<byte>) =
  let buf = buffer img
  let mutable contours = List.empty
  let mutable contourId = 1
  for y in 1 .. img.Height - 1 do
    for x in 1 .. img.Width - 1 do
      if buf.keys.Rep.[y, x] = 0 && img.Rep.[y, x] <> 255uy then
        contours <- (single buf (y, x) contourId img) :: contours
        contourId <- contourId + 1

  contours, (buf.edges, buf.keys)

let recover (contours: Contour list) =
  let height =
    contours
    |> List.map (fun { region = (_, (h, _)) } -> h)
    |> List.max

  let width =
    contours
    |> List.map (fun { region = (_, (_, w)) } -> w)
    |> List.max

  let img =
    Matrix.Matrix<byte>(width + 1, height + 1)

  let filler (contour: Contour) =
    let mutable y = 0
    for n in 0 .. contour.edges.Length - 1 do
      let e = contour.edges.[n]
      if (e &&& 0x8000us) <> 0us then
        y <- int (e &&& ~~~0x8000us)
      else
        img.Rep.[y, (int e)] <- contour.color

  contours |> List.iter filler

  img

let encode (contour: Contour) =
  let ((top, left), _) = contour.region

  let buffer =
    Array.init (contour.edges.Length * 2) (fun _ -> 0uy)

  Buffer.BlockCopy(contour.edges, 0, buffer, 0, buffer.Length)

  use hasher = new SHA256Managed()
  let fid = Feature.Fid(hasher.ComputeHash(buffer))

  Feature.Handle
    (fid,
     { area = contour.area
       position = (top, left) })
