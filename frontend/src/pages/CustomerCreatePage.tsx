import { useState, useRef } from 'react';
import type { ChangeEvent, FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import PageContainer from '../components/PageContainer';
import ErrorMessage from '../components/ErrorMessage';
import { createCustomer, lookupZipCode } from '../api/customersCommands';
import type { CreateCustomerCommand } from '../api/customersCommands';
import { HttpError } from '../api/httpClient';
import type { CustomerType } from '../api/types';

interface FormFields {
  type: CustomerType;
  name: string;
  document: string;
  birthOrFoundationDate: string;
  email: string;
  phone: string;
  zipCode: string;
  street: string;
  number: string;
  district: string;
  city: string;
  state: string;
  stateRegistration: string;
  isStateRegistrationExempt: boolean;
}

type FormErrors = Partial<Record<keyof FormFields, string>>;

const INITIAL: FormFields = {
  type: 'Individual',
  name: '',
  document: '',
  birthOrFoundationDate: '',
  email: '',
  phone: '',
  zipCode: '',
  street: '',
  number: '',
  district: '',
  city: '',
  state: '',
  stateRegistration: '',
  isStateRegistrationExempt: false,
};

function isAtLeast18(dateStr: string): boolean {
  const birth = new Date(`${dateStr}T00:00:00`);
  const limit = new Date();
  limit.setFullYear(limit.getFullYear() - 18);
  return birth <= limit;
}

// Validações básicas antecipam erro sem substituir regras do domínio.
function validate(f: FormFields): FormErrors {
  const e: FormErrors = {};

  if (!f.name.trim())     e.name     = 'Nome é obrigatório.';
  if (!f.document.trim()) e.document = 'Documento é obrigatório.';

  if (!f.birthOrFoundationDate) {
    e.birthOrFoundationDate = f.type === 'Individual'
      ? 'Data de nascimento é obrigatória.'
      : 'Data de fundação é obrigatória.';
  } else if (f.type === 'Individual' && !isAtLeast18(f.birthOrFoundationDate)) {
    e.birthOrFoundationDate = 'Cliente deve ter ao menos 18 anos.';
  }

  if (!f.email.trim()) {
    e.email = 'E-mail é obrigatório.';
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.email)) {
    e.email = 'E-mail inválido.';
  }

  if (!f.phone.trim()) e.phone = 'Telefone é obrigatório.';

  if (!f.zipCode.replace(/\D/g, '')) e.zipCode  = 'CEP é obrigatório.';
  if (!f.street.trim())              e.street   = 'Logradouro é obrigatório.';
  if (!f.number.trim())              e.number   = 'Número é obrigatório.';
  if (!f.district.trim())            e.district = 'Bairro é obrigatório.';
  if (!f.city.trim())                e.city     = 'Cidade é obrigatória.';
  if (!f.state.trim())               e.state    = 'UF é obrigatória.';

  if (f.type === 'Company' && !f.isStateRegistrationExempt && !f.stateRegistration.trim()) {
    e.stateRegistration = 'IE obrigatória quando não houver isenção.';
  }

  return e;
}

export default function CustomerCreatePage() {
  const navigate = useNavigate();
  const [fields, setFields]           = useState<FormFields>(INITIAL);
  const [errors, setErrors]           = useState<FormErrors>({});
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting]   = useState(false);
  const [zipLoading, setZipLoading]   = useState(false);
  const errorBannerRef                = useRef<HTMLDivElement>(null);

  // Retorna um onChange controlado que limpa o erro do campo ao editar.
  function str(key: keyof FormFields) {
    return (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
      const value = key === 'state' ? e.target.value.toUpperCase() : e.target.value;
      setFields(prev => ({ ...prev, [key]: value } as FormFields));
      setErrors(prev => ({ ...prev, [key]: undefined }));
    };
  }

  async function handleZipBlur() {
    const digits = fields.zipCode.replace(/\D/g, '');
    if (digits.length !== 8) return;
    setZipLoading(true);
    try {
      const addr = await lookupZipCode(digits);
      setFields(prev => ({
        ...prev,
        street:   addr.street   || prev.street,
        district: addr.district || prev.district,
        city:     addr.city     || prev.city,
        state:    addr.state    || prev.state,
      }));
      setErrors(prev => ({
        ...prev,
        street: undefined, district: undefined, city: undefined, state: undefined,
      }));
    } catch {
      // Falha silenciosa — usuário preenche manualmente.
    } finally {
      setZipLoading(false);
    }
  }

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();

    const errs = validate(fields);
    if (Object.keys(errs).length > 0) {
      setErrors(errs);
      return;
    }

    setSubmitting(true);
    setSubmitError(null);

    const payload: CreateCustomerCommand = {
      type:                      fields.type,
      name:                      fields.name.trim(),
      document:                  fields.document.replace(/\D/g, ''),
      birthOrFoundationDate:     fields.birthOrFoundationDate,
      email:                     fields.email.trim(),
      phone:                     fields.phone.trim(),
      zipCode:                   fields.zipCode.replace(/\D/g, ''),
      street:                    fields.street.trim(),
      number:                    fields.number.trim(),
      district:                  fields.district.trim(),
      city:                      fields.city.trim(),
      state:                     fields.state.trim().toUpperCase(),
      stateRegistration:         fields.stateRegistration.trim() || undefined,
      isStateRegistrationExempt: fields.isStateRegistrationExempt,
    };

    try {
      const id = await createCustomer(payload);
      void navigate(`/customers/${id}`);
    } catch (err) {
      const detail = err instanceof HttpError ? ` (${err.status})` : '';
      setSubmitError(`Não foi possível salvar o cliente${detail}. Verifique os dados e tente novamente.`);
      setSubmitting(false);
      setTimeout(() => errorBannerRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' }), 50);
    }
  }

  const isPF = fields.type === 'Individual';

  return (
    <PageContainer title="Cadastrar Cliente">
      <div className="detail-header">
        <Link to="/customers" className="btn-secondary">← Lista de clientes</Link>
      </div>

      <form onSubmit={e => void handleSubmit(e)} noValidate>

        {/* ---- Dados básicos ---- */}
        <section className="form-section">
          <h2 className="form-section-title">Dados básicos</h2>
          <div className="form-grid">

            <div className="form-group">
              <label className="form-label" htmlFor="type">
                Tipo <span className="required">*</span>
              </label>
              <select id="type" className="form-input" value={fields.type} onChange={str('type')}>
                <option value="Individual">Pessoa Física (PF)</option>
                <option value="Company">Pessoa Jurídica (PJ)</option>
              </select>
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="name">
                {isPF ? 'Nome completo' : 'Razão social'} <span className="required">*</span>
              </label>
              <input
                id="name"
                className={`form-input${errors.name ? ' has-error' : ''}`}
                type="text"
                autoComplete="name"
                value={fields.name}
                onChange={str('name')}
                aria-describedby={errors.name ? 'name-error' : undefined}
                aria-invalid={errors.name ? true : undefined}
              />
              {errors.name && <span id="name-error" className="form-error">{errors.name}</span>}
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="document">
                {isPF ? 'CPF' : 'CNPJ'} <span className="required">*</span>
              </label>
              <input
                id="document"
                className={`form-input${errors.document ? ' has-error' : ''}`}
                type="text"
                inputMode="numeric"
                value={fields.document}
                onChange={str('document')}
                aria-describedby={errors.document ? 'document-error' : undefined}
                aria-invalid={errors.document ? true : undefined}
              />
              {errors.document && (
                <span id="document-error" className="form-error">{errors.document}</span>
              )}
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="birthDate">
                {isPF ? 'Data de nascimento' : 'Data de fundação'} <span className="required">*</span>
              </label>
              <input
                id="birthDate"
                className={`form-input${errors.birthOrFoundationDate ? ' has-error' : ''}`}
                type="date"
                value={fields.birthOrFoundationDate}
                onChange={str('birthOrFoundationDate')}
                aria-describedby={errors.birthOrFoundationDate ? 'date-error' : undefined}
                aria-invalid={errors.birthOrFoundationDate ? true : undefined}
              />
              {errors.birthOrFoundationDate && (
                <span id="date-error" className="form-error">{errors.birthOrFoundationDate}</span>
              )}
            </div>

          </div>
        </section>

        {/* ---- Contato ---- */}
        <section className="form-section">
          <h2 className="form-section-title">Contato</h2>
          <div className="form-grid">

            <div className="form-group">
              <label className="form-label" htmlFor="email">
                E-mail <span className="required">*</span>
              </label>
              <input
                id="email"
                className={`form-input${errors.email ? ' has-error' : ''}`}
                type="email"
                autoComplete="email"
                value={fields.email}
                onChange={str('email')}
                aria-describedby={errors.email ? 'email-error' : undefined}
                aria-invalid={errors.email ? true : undefined}
              />
              {errors.email && <span id="email-error" className="form-error">{errors.email}</span>}
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="phone">
                Telefone <span className="required">*</span>
              </label>
              <input
                id="phone"
                className={`form-input${errors.phone ? ' has-error' : ''}`}
                type="tel"
                autoComplete="tel"
                value={fields.phone}
                onChange={str('phone')}
                aria-describedby={errors.phone ? 'phone-error' : undefined}
                aria-invalid={errors.phone ? true : undefined}
              />
              {errors.phone && <span id="phone-error" className="form-error">{errors.phone}</span>}
            </div>

          </div>
        </section>

        {/* ---- Endereço ---- */}
        <section className="form-section">
          <h2 className="form-section-title">Endereço</h2>
          <div className="form-grid">

            <div className="form-group">
              <label className="form-label" htmlFor="zipCode">
                CEP <span className="required">*</span>
              </label>
              <input
                id="zipCode"
                className={`form-input${errors.zipCode ? ' has-error' : ''}`}
                type="text"
                inputMode="numeric"
                maxLength={9}
                value={fields.zipCode}
                onChange={str('zipCode')}
                onBlur={() => void handleZipBlur()}
                aria-describedby={errors.zipCode ? 'zip-error' : undefined}
                aria-invalid={errors.zipCode ? true : undefined}
              />
              {errors.zipCode && <span id="zip-error" className="form-error">{errors.zipCode}</span>}
              {zipLoading && <span className="form-hint">Consultando CEP…</span>}
            </div>

            <div className="form-group full">
              <label className="form-label" htmlFor="street">
                Logradouro <span className="required">*</span>
              </label>
              <input
                id="street"
                className={`form-input${errors.street ? ' has-error' : ''}`}
                type="text"
                value={fields.street}
                onChange={str('street')}
                aria-describedby={errors.street ? 'street-error' : undefined}
                aria-invalid={errors.street ? true : undefined}
              />
              {errors.street && <span id="street-error" className="form-error">{errors.street}</span>}
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="number">
                Número <span className="required">*</span>
              </label>
              <input
                id="number"
                className={`form-input${errors.number ? ' has-error' : ''}`}
                type="text"
                value={fields.number}
                onChange={str('number')}
                aria-describedby={errors.number ? 'number-error' : undefined}
                aria-invalid={errors.number ? true : undefined}
              />
              {errors.number && <span id="number-error" className="form-error">{errors.number}</span>}
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="district">
                Bairro <span className="required">*</span>
              </label>
              <input
                id="district"
                className={`form-input${errors.district ? ' has-error' : ''}`}
                type="text"
                value={fields.district}
                onChange={str('district')}
                aria-describedby={errors.district ? 'district-error' : undefined}
                aria-invalid={errors.district ? true : undefined}
              />
              {errors.district && (
                <span id="district-error" className="form-error">{errors.district}</span>
              )}
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="city">
                Cidade <span className="required">*</span>
              </label>
              <input
                id="city"
                className={`form-input${errors.city ? ' has-error' : ''}`}
                type="text"
                value={fields.city}
                onChange={str('city')}
                aria-describedby={errors.city ? 'city-error' : undefined}
                aria-invalid={errors.city ? true : undefined}
              />
              {errors.city && <span id="city-error" className="form-error">{errors.city}</span>}
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="state">
                UF <span className="required">*</span>
              </label>
              <input
                id="state"
                className={`form-input${errors.state ? ' has-error' : ''}`}
                type="text"
                maxLength={2}
                value={fields.state}
                onChange={str('state')}
                aria-describedby={errors.state ? 'state-error' : undefined}
                aria-invalid={errors.state ? true : undefined}
              />
              {errors.state && <span id="state-error" className="form-error">{errors.state}</span>}
            </div>

          </div>
        </section>

        {/* ---- Tributação (PJ) ---- */}
        {fields.type === 'Company' && (
          <section className="form-section">
            <h2 className="form-section-title">Tributação</h2>
            <div className="form-grid">

              <div className="form-group full">
                <div className="form-checkbox-row">
                  <input
                    id="isExempt"
                    type="checkbox"
                    checked={fields.isStateRegistrationExempt}
                    onChange={e => {
                      setFields(prev => ({ ...prev, isStateRegistrationExempt: e.target.checked }));
                      setErrors(prev => ({ ...prev, stateRegistration: undefined }));
                    }}
                  />
                  <label className="form-label" htmlFor="isExempt">
                    Isento de Inscrição Estadual (IE)
                  </label>
                </div>
              </div>

              {!fields.isStateRegistrationExempt && (
                <div className="form-group">
                  <label className="form-label" htmlFor="stateRegistration">
                    Inscrição Estadual <span className="required">*</span>
                  </label>
                  <input
                    id="stateRegistration"
                    className={`form-input${errors.stateRegistration ? ' has-error' : ''}`}
                    type="text"
                    value={fields.stateRegistration}
                    onChange={str('stateRegistration')}
                    aria-describedby={errors.stateRegistration ? 'ie-error' : undefined}
                    aria-invalid={errors.stateRegistration ? true : undefined}
                  />
                  {errors.stateRegistration && (
                    <span id="ie-error" className="form-error">{errors.stateRegistration}</span>
                  )}
                </div>
              )}

            </div>
          </section>
        )}

        {/* ---- Erro de submissão ---- */}
        {submitError && (
          <div ref={errorBannerRef} className="form-submit-error">
            <ErrorMessage message={submitError} />
          </div>
        )}

        {/* ---- Ações ---- */}
        <div className="form-actions">
          <button
            type="button"
            className="btn-secondary"
            onClick={() => void navigate('/customers')}
          >
            Cancelar
          </button>
          <button
            type="submit"
            className="btn-primary"
            disabled={submitting}
            aria-busy={submitting}
          >
            {submitting ? 'Salvando…' : 'Salvar cliente'}
          </button>
        </div>

      </form>
    </PageContainer>
  );
}
