import { yearlySavingPercent } from './plans';

describe('yearlySavingPercent', () => {
  it('compares the yearly price with twelve monthly payments', () => {
    // 12 x 49 = 588; 499 is about 15% less.
    expect(yearlySavingPercent({ monthlyPriceInr: 49, yearlyPriceInr: 499 })).toBe(15);
  });

  it('is zero when yearly is not cheaper, such as during a monthly-only offer', () => {
    expect(yearlySavingPercent({ monthlyPriceInr: 9, yearlyPriceInr: 499 })).toBe(0);
    expect(yearlySavingPercent({ monthlyPriceInr: 50, yearlyPriceInr: 600 })).toBe(0);
  });

  it('is zero for a free plan', () => {
    expect(yearlySavingPercent({ monthlyPriceInr: 0, yearlyPriceInr: 0 })).toBe(0);
  });
});
