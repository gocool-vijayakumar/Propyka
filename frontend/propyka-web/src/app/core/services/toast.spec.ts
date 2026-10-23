import { TestBed } from '@angular/core/testing';
import { Toasts } from './toast';

describe('Toasts', () => {

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({});
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('shows a message and clears it after its duration', () => {
    const toasts = TestBed.inject(Toasts);

    toasts.success('Saved');
    expect(toasts.items().map(t => t.message)).toEqual(['Saved']);

    vi.advanceTimersByTime(4600);
    expect(toasts.items()).toEqual([]);
  });

  it('keeps only the four most recent messages', () => {
    const toasts = TestBed.inject(Toasts);

    ['a', 'b', 'c', 'd', 'e'].forEach(message => toasts.show(message));

    expect(toasts.items().map(t => t.message)).toEqual(['b', 'c', 'd', 'e']);
  });

  it('dismisses a message on demand', () => {
    const toasts = TestBed.inject(Toasts);

    const id = toasts.error('Something went wrong');
    toasts.dismiss(id);

    expect(toasts.items()).toEqual([]);
  });
});
