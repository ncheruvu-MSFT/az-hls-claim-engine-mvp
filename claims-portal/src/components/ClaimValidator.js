import React, { useState } from 'react';
import axios from 'axios';

function ClaimValidator({ plans, apiBase }) {
  const [formData, setFormData] = useState({
    planId: 'PLAN001',
    serviceCode: '99213',
    providerId: 'PROV001',
    claimAmount: 500,
    yearToDateSpent: 300
  });
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(false);

  const validateClaim = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      const response = await axios.post(`${apiBase}/validate-claim`, formData);
      setResult(response.data);
    } catch (error) {
      setResult({ IsApproved: false, Errors: ['Error validating claim'] });
    }
    setLoading(false);
  };

  return (
    <div className="card">
      <h2>Claim Validation</h2>
      <p>Submit a claim for real-time validation against benefit rules</p>

      <form onSubmit={validateClaim}>
        <div className="form-group">
          <label>Select Plan</label>
          <select
            value={formData.planId}
            onChange={(e) => setFormData({ ...formData, planId: e.target.value })}
          >
            {plans.map(plan => (
              <option key={plan.PlanId} value={plan.PlanId}>
                {plan.PlanName} ({plan.PlanType})
              </option>
            ))}
          </select>
        </div>

        <div className="form-group">
          <label>Service Code (CPT)</label>
          <input
            type="text"
            value={formData.serviceCode}
            onChange={(e) => setFormData({ ...formData, serviceCode: e.target.value })}
            placeholder="e.g., 99213"
            required
          />
        </div>

        <div className="form-group">
          <label>Provider ID</label>
          <input
            type="text"
            value={formData.providerId}
            onChange={(e) => setFormData({ ...formData, providerId: e.target.value })}
            placeholder="e.g., PROV001"
            required
          />
        </div>

        <div className="form-group">
          <label>Claim Amount ($)</label>
          <input
            type="number"
            value={formData.claimAmount}
            onChange={(e) => setFormData({ ...formData, claimAmount: parseFloat(e.target.value) })}
            placeholder="500"
            required
          />
        </div>

        <div className="form-group">
          <label>Year-to-Date Spent ($)</label>
          <input
            type="number"
            value={formData.yearToDateSpent}
            onChange={(e) => setFormData({ ...formData, yearToDateSpent: parseFloat(e.target.value) })}
            placeholder="300"
            required
          />
        </div>

        <button type="submit" className="btn" disabled={loading}>
          {loading ? 'Validating...' : '⚖️ Validate Claim'}
        </button>
      </form>

      {result && (
        <div className={`result ${result.IsApproved ? 'success' : 'error'}`}>
          <h3>{result.IsApproved ? '✓ Claim Approved' : '✗ Claim Denied'}</h3>
          
          {result.Errors.length > 0 && (
            <div style={{ marginTop: '15px' }}>
              <strong style={{ color: '#dc3545' }}>Errors:</strong>
              <ul>
                {result.Errors.map((err, i) => <li key={i}>{err}</li>)}
              </ul>
            </div>
          )}

          {result.Warnings.length > 0 && (
            <div style={{ marginTop: '15px' }}>
              <strong style={{ color: '#ff9800' }}>Warnings:</strong>
              <ul>
                {result.Warnings.map((warn, i) => <li key={i}>{warn}</li>)}
              </ul>
            </div>
          )}

          {result.DenialReason && (
            <div style={{ marginTop: '15px' }}>
              <strong>Denial Reason:</strong> {result.DenialReason}
            </div>
          )}

          <div className="stats" style={{ marginTop: '20px' }}>
            <div className="stat-item">
              <div className="stat-value">${result.PatientResponsibility.toFixed(2)}</div>
              <div className="stat-label">Patient Responsibility</div>
            </div>
            <div className="stat-item">
              <div className="stat-value">${result.InsurancePayment.toFixed(2)}</div>
              <div className="stat-label">Insurance Payment</div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default ClaimValidator;
