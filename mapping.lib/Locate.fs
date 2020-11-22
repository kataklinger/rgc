module Locate

open System.Collections.Generic

type Environment =
  { matches: Sketch.Matches list
    range: Range.Range
    wnd: Primitive.Rect
    previous: int * int }

let private encode (y: int, x: int) =
  ((uint64 y) <<< 32) ||| (uint64 (uint32 x))

let private decode (pos: uint64) =
  int (pos >>> 32), int (pos &&& 0xffffffffUL)

let byFeatureArea (env: Environment) =
  let totals = new Dictionary<uint64, int>()
  for matched in env.matches do
    match matched with
    | { matched = Some known; key = _; feature = feature } ->
        for pos in known do
          if pos.area = feature.area then
            let key =
              encode (Primitive.sub pos.position feature.position)

            let mutable total = 0
            match totals.TryGetValue(key, &total) with
            | true -> totals.[key] <- total + feature.area
            | _ -> totals.Add(key, feature.area)
    | _ -> ()

  match totals.Count with
  | 0 -> None
  | _ ->
      let best = totals |> Seq.maxBy (fun i -> i.Value)
      Some(decode best.Key)

let byFeatureCount (env: Environment) =
  let totals = new Dictionary<uint64, int>()
  for matched in env.matches do
    match matched with
    | { matched = Some known; key = _; feature = feature } ->
        for pos in known do
          if pos.area = feature.area then
            let key =
              encode (Primitive.sub pos.position feature.position)

            let mutable total = 0
            match totals.TryGetValue(key, &total) with
            | true -> totals.[key] <- total + 1
            | _ -> totals.Add(key, 1)
    | _ -> ()

  let checkPosition position count =
    let rangeCount =
      Range.count (Primitive.Rect.move position env.wnd) env.range

    match (float count) / (float rangeCount) with
    | x when x > 0.5 -> Some position
    | _ -> None

  match totals.Count with
  | 0 -> None
  | _ ->
      let position, count, _ =
        totals
        |> Seq.map (fun item ->
             let position = decode item.Key
             let count = item.Value

             let distance =
               position |> Primitive.Point.distance env.previous

             let delta =
               (distance |> max 1.f |> log |> min 3.f)
               * 0.8f
               / 3.f

             let score = (float32 count) * (1.f - delta)

             (position, count, score))
        |> Seq.maxBy (fun (_, _, score) -> score)

      checkPosition position count
