import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { Admin } from './admin';

describe('Admin', () => {
  let service: Admin;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(Admin);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
