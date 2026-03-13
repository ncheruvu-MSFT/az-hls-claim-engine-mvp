import React, { useState } from 'react';
import axios from 'axios';

function EligibilityChecker({ plans, apiBase }) {
  const [patientId, setPatientId] = useState('PAT001');
  const [planId, setPlanId] = useState('PLAN001');
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(false);

  const checkEligibility = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      const response = await axios.get(`${apiBase}/check-eligibility`, {
        params: { patientId, planId }
      });
      setResult(response.data);
    } catch (error) {
      setResult({ IsEligible: false, Reason: 'Error checking eligibility' });
    }
    setLoading(false);
  };

  return (
    <div className="card">
      <h2>Eligibility Verification</h2>
      <p>Check if a patient is eligible for coverage under a specific plan</p>

      <form onSubmit={checkEligibility}>
        <div className="form-group">
          <label>Patient ID</label>
          <input
            type="text"
            value={patientId}
            onChange={(e) => setPatientId(e.target.value)}
            placeholder="Enter patient ID"
            required
          />
        </div>

        <div className="form-group">
          <label>Select Plan</label>
          <select value={planId} onChange={(e) => setPlanId(e.target.value)}>
            {plans.map(plan => (
              <option key={plan.PlanId} value={plan.PlanId}>
                {plan.PlanName} ({plan.PlanType})
              </option>
            ))}
          </select>
        </div>

        <button type="submit" className="btn" disabled={loading}>
          {loading ? 'Checking...' : '🔍 Check Eligibility'}
        </button>
      </form>

      {result && (
        <div className={`result ${result.IsEligible ? 'success' : 'error'}`}>
          <h3>{result.IsEligible ? '✓ Eligible' : '✗ Not Eligible'}</h3>
          <p><strong>Reason:</strong> {result.Reason}</p>
          {result.IsEligible && (
            <>
              <div className="stats" style={{ marginTop: '20px' }}>
                <div className="stat-item">
                  <div className="stat-value">${result.RemainingDeductible.toLocaleString()}</div>
                  <div className="stat-label">Remaining Deductible</div>
                </div>
                <div className="stat-item">
                  <div className="stat-value">${result.YearToDateClaims.toLocaleString()}</div>
                  <div className="stat-label">YTD Claims</div>
                </div>
              </div>
            </>
          )}
        </div>
      )}
    </div>
  );
}

export default EligibilityChecker;
