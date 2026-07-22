import { FormControl, FormGroup, Validators } from '@angular/forms';
import { CustomerDetails } from './customer.model';

export type CustomerForm = FormGroup<{
  title: FormControl<string>;
  firstName: FormControl<string>;
  lastName: FormControl<string>;
  dateOfBirth: FormControl<string>;
  addressLine1: FormControl<string>;
  addressLine2: FormControl<string>;
  city: FormControl<string>;
  region: FormControl<string>;
  postalCode: FormControl<string>;
  countryCode: FormControl<string>;
  email: FormControl<string>;
  phone: FormControl<string>;
}>;

export function createCustomerForm(): CustomerForm {
  const required = [Validators.required];
  return new FormGroup({
    title: new FormControl('Ms', { nonNullable: true, validators: required }),
    firstName: new FormControl('', { nonNullable: true, validators: required }),
    lastName: new FormControl('', { nonNullable: true, validators: required }),
    dateOfBirth: new FormControl('', { nonNullable: true, validators: required }),
    addressLine1: new FormControl('', { nonNullable: true, validators: required }),
    addressLine2: new FormControl('', { nonNullable: true }),
    city: new FormControl('', { nonNullable: true, validators: required }),
    region: new FormControl('', { nonNullable: true }),
    postalCode: new FormControl('', { nonNullable: true, validators: required }),
    countryCode: new FormControl('GB', { nonNullable: true, validators: [Validators.required, Validators.pattern(/^[A-Za-z]{2}$/)] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    phone: new FormControl('', { nonNullable: true })
  });
}

export function formToDetails(form: CustomerForm): CustomerDetails {
  const value = form.getRawValue();
  return {
    ...value,
    addressLine2: value.addressLine2 || null,
    region: value.region || null,
    phone: value.phone || null
  };
}
