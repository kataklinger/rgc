module Feature

type Fid = bigint

type Feature =
  { area: int
    position: Primitive.Point }

type Handle = Fid * Feature
