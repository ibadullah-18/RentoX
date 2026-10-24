import { describe, expect, it } from "vitest";
import { clampOffset, coverScale, fitFrame, leavesGap, outputSize, turned, zoomLimits } from "./image-crop";

describe("image-crop", () => {
  it("fits the frame inside the stage with a margin", () => {
    const f = fitFrame({ w: 500, h: 400 }, 1);
    expect(f.w).toBeCloseTo(368);
    expect(f.h).toBeCloseTo(368);
    const wide = fitFrame({ w: 500, h: 400 }, 3);
    expect(wide.w).toBeCloseTo(468);
    expect(wide.h).toBeCloseTo(156);
  });

  it("swaps the sides on a quarter turn only", () => {
    expect(turned({ w: 400, h: 300 }, 0)).toEqual({ w: 400, h: 300 });
    expect(turned({ w: 400, h: 300 }, 1)).toEqual({ w: 300, h: 400 });
    expect(turned({ w: 400, h: 300 }, 2)).toEqual({ w: 400, h: 300 });
  });

  it("covers the frame exactly at the smallest size", () => {
    const s = coverScale({ w: 1000, h: 500 }, { w: 200, h: 200 });
    expect(s).toBeCloseTo(0.4);
    // the shorter side just fills: no empty edge, and no room to move on that axis
    expect(clampOffset({ x: 50, y: 50 }, { w: 1000, h: 500 }, { w: 200, h: 200 }, s)).toEqual({ x: 50, y: 0 });
  });

  it("never lets an empty edge show", () => {
    const c = clampOffset({ x: 999, y: -999 }, { w: 1000, h: 1000 }, { w: 200, h: 200 }, 0.5);
    expect(c).toEqual({ x: 150, y: -150 });
  });

  it("limits the zoom so the result stays sharp", () => {
    const small = zoomLimits({ w: 400, h: 400 }, { w: 300, h: 300 }, 512);
    expect(small.min).toBe(1);
    expect(small.max).toBeGreaterThanOrEqual(1.5);
    const big = zoomLimits({ w: 4000, h: 3000 }, { w: 300, h: 300 }, 512);
    expect(big.max).toBeLessThanOrEqual(8);
  });

  it("lets the whole picture be fitted inside the frame", () => {
    // a wide picture in a square frame: it can shrink to half of "covering" before it fits across
    expect(zoomLimits({ w: 1000, h: 500 }, { w: 200, h: 200 }, 512).min).toBeCloseTo(0.5);
    // the same picture when fitting is not wanted
    expect(zoomLimits({ w: 1000, h: 500 }, { w: 200, h: 200 }, 512, false).min).toBe(1);
    // a picture of the frame's own shape cannot get smaller than the frame
    expect(zoomLimits({ w: 600, h: 200 }, { w: 300, h: 100 }, 900).min).toBe(1);
  });

  it("tells when the frame has an empty part", () => {
    expect(leavesGap({ w: 1000, h: 500 }, { w: 200, h: 200 }, 0.4)).toBe(false);
    expect(leavesGap({ w: 1000, h: 500 }, { w: 200, h: 200 }, 0.3)).toBe(true);
  });

  it("never makes a picture bigger than the photo can give", () => {
    expect(outputSize({ w: 300, h: 300 }, 1, 512, 1)).toEqual({ w: 300, h: 300 });
    expect(outputSize({ w: 300, h: 300 }, 0.2, 512, 1)).toEqual({ w: 512, h: 512 });
    expect(outputSize({ w: 600, h: 200 }, 0.5, 1500, 3)).toEqual({ w: 1200, h: 400 });
  });
});
