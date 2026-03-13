import React from 'react';

function BenefitsView({ plans }) {
  return (
    <div className="card">
      <h2>Available Benefit Plans</h2>
      <p>Choose the plan that best fits your healthcare needs</p>

      {plans.map(plan => (
        <div key={plan.PlanId} className="plan-card">
          <h3>{plan.PlanName} ({plan.PlanType})</h3>
          <div className="stats">
            <div className="stat-item">
              <div className="stat-value">${plan.Deductible.toLocaleString()}</div>
              <div className="stat-label">Annual Deductible</div>
            </div>
            <div className="stat-item">
              <div className="stat-value">${plan.OutOfPocketMax.toLocaleString()}</div>
              <div className="stat-label">Out-of-Pocket Max</div>
            </div>
            <div className="stat-item">
              <div className="stat-value">${plan.Copay}</div>
              <div className="stat-label">Office Visit Copay</div>
            </div>
            <div className="stat-item">
              <div className="stat-value">{(plan.CoinsuranceRate * 100).toFixed(0)}%</div>
              <div className="stat-label">Coinsurance</div>
            </div>
          </div>

          <div style={{ marginTop: '20px' }}>
            <strong>Covered Services (CPT codes):</strong>
            <div style={{ marginTop: '8px' }}>
              {plan.CoveredServices.map(code => (
                <span key={code} style={{
                  display: 'inline-block',
                  padding: '4px 12px',
                  margin: '4px',
                  background: '#e7f3ff',
                  borderRadius: '16px',
                  fontSize: '14px'
                }}>
                  {code}
                </span>
              ))}
            </div>
          </div>

          <div style={{ marginTop: '15px' }}>
            <strong>Network Providers:</strong>
            <div style={{ marginTop: '8px' }}>
              {plan.NetworkProviders.map(prov => (
                <span key={prov} style={{
                  display: 'inline-block',
                  padding: '4px 12px',
                  margin: '4px',
                  background: '#e8f5e9',
                  borderRadius: '16px',
                  fontSize: '14px'
                }}>
                  {prov}
                </span>
              ))}
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}

export default BenefitsView;
