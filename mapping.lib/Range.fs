module Range

type Point =
  { position: Primitive.Point
    count: int }

type Range = { nodes: (int * int) Set }

let empty = { nodes = Set.empty }

let update (feature: Feature.Feature) (range: Range) =
  { range with
      nodes = range.nodes |> Set.add feature.position }

let count ((top: int, left: int), (bottom: int, right: int)) (range: Range) =
  range.nodes
  |> Seq.filter (fun (y, x) -> y >= top && y <= bottom && x >= left && x <= right)
  |> Seq.length
