module Sketch


type Confirmation = { count: int; stamp: int }

type Sketch =
  { stamp: int
    confirmed: Map<Feature.Fid, Set<Feature.Feature>>
    pending: Map<Feature.Handle, Confirmation>
    range: Range.Range }

let empty =
  { stamp = 0
    confirmed = Map.empty
    pending = Map.empty
    range = Range.empty }

type Matches =
  { matched: Set<Feature.Feature> option
    key: Feature.Fid
    feature: Feature.Feature }

let find (sketch: Sketch) (features: Feature.Handle list) =
  features
  |> List.map (fun (key, feature) ->
       match sketch.confirmed |> Map.tryFind key with
       | Some known ->
           { matched = Some known
             key = key
             feature = feature }
       | _ ->
           { matched = None
             key = key
             feature = feature })

let update (sketch: Sketch) (pos: Primitive.Point) (features: Feature.Handle list) =
  let nextStamp = sketch.stamp + 1

  let folder (state: Sketch) (fid: Feature.Fid, feature: Feature.Feature) =
    let adjusted =
      fid,
      { feature with
          position = Primitive.add feature.position pos }

    let addPending w =
      state.pending
      |> Map.add adjusted { count = w; stamp = nextStamp }

    let addConfirmed () =
      let confirmed =
        Set.empty
        |> defaultArg (state.confirmed |> Map.tryFind fid)
        |> Set.add (snd adjusted)

      state.confirmed |> Map.add fid confirmed

    let updateRange () =
      state.range |> Range.update (snd adjusted)

    match state.pending |> Map.tryFind adjusted with
    | Some confirm when confirm.count >= 25 -> state
    | Some confirm when confirm.stamp = sketch.stamp ->
        let pending =
          state.pending
          |> Map.add
               adjusted
               { confirm with
                   count = confirm.count + 1
                   stamp = nextStamp }

        match confirm.count + 1 with
        | 25 ->
            { state with
                confirmed = addConfirmed ()
                pending = pending
                range = updateRange () }
        | _ -> { state with pending = pending }
    | _ when sketch.stamp = 0 ->
        { state with
            confirmed = addConfirmed ()
            pending = addPending 25
            range = updateRange () }
    | _ -> { state with pending = addPending 1 }

  let newSketch = features |> List.fold folder sketch

  { newSketch with stamp = nextStamp }
