# Sample FHIR Data for Testing Epic-Style Screens

Load these resources into your Azure FHIR server: `https://ahdswstest001-fhirr4test001.fhir.azurehealthcareapis.com`

## How to Load Data

### Using Postman:
```http
POST {{fhirUrl}}/Patient
Authorization: Bearer {{token}}
Content-Type: application/fhir+json

{... patient JSON ...}
```

### Using Azure CLI:
```bash
# Get access token
TOKEN=$(az account get-access-token --resource=https://ahdswstest001-fhirr4test001.fhir.azurehealthcareapis.com --query accessToken -o tsv)

# Upload patient
curl -X POST "https://ahdswstest001-fhirr4test001.fhir.azurehealthcareapis.com/Patient" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/fhir+json" \
  -d @patient.json
```

---

## 1. Patient Resource

```json
{
  "resourceType": "Patient",
  "id": "example-patient-1",
  "identifier": [
    {
      "system": "http://claimsiq.com/member-id",
      "value": "SUB001"
    },
    {
      "system": "http://hl7.org/fhir/sid/us-ssn",
      "value": "123-45-6789"
    }
  ],
  "active": true,
  "name": [
    {
      "use": "official",
      "family": "Smith",
      "given": ["John", "Michael"]
    }
  ],
  "telecom": [
    {
      "system": "phone",
      "value": "(555) 123-4567",
      "use": "home"
    },
    {
      "system": "email",
      "value": "john.smith@email.com"
    }
  ],
  "gender": "male",
  "birthDate": "1980-01-15",
  "address": [
    {
      "use": "home",
      "line": ["123 Main Street"],
      "city": "Los Angeles",
      "state": "CA",
      "postalCode": "90001",
      "country": "US"
    }
  ]
}
```

---

## 2. Practitioner Resources

### Dr. Sarah Johnson (Primary Care)
```json
{
  "resourceType": "Practitioner",
  "id": "dr-johnson",
  "identifier": [
    {
      "system": "http://hl7.org/fhir/sid/us-npi",
      "value": "1234567890"
    }
  ],
  "active": true,
  "name": [
    {
      "use": "official",
      "family": "Johnson",
      "given": ["Sarah"],
      "prefix": ["Dr."],
      "suffix": ["MD"]
    }
  ],
  "telecom": [
    {
      "system": "phone",
      "value": "(555) 987-6543",
      "use": "work"
    }
  ],
  "address": [
    {
      "use": "work",
      "line": ["456 Medical Plaza"],
      "city": "Los Angeles",
      "state": "CA",
      "postalCode": "90001"
    }
  ],
  "qualification": [
    {
      "code": {
        "coding": [
          {
            "system": "http://terminology.hl7.org/CodeSystem/v2-0360",
            "code": "MD",
            "display": "Doctor of Medicine"
          }
        ],
        "text": "Doctor of Medicine"
      }
    }
  ]
}
```

### Dr. Michael Chen (Cardiologist)
```json
{
  "resourceType": "Practitioner",
  "id": "dr-chen",
  "identifier": [
    {
      "system": "http://hl7.org/fhir/sid/us-npi",
      "value": "9876543210"
    }
  ],
  "active": true,
  "name": [
    {
      "use": "official",
      "family": "Chen",
      "given": ["Michael"],
      "prefix": ["Dr."],
      "suffix": ["MD"]
    }
  ],
  "qualification": [
    {
      "code": {
        "coding": [
          {
            "system": "http://nucc.org/provider-taxonomy",
            "code": "207RC0000X",
            "display": "Cardiovascular Disease"
          }
        ]
      }
    }
  ]
}
```

---

## 3. Medication Request Resources

### Metformin 500mg
```json
{
  "resourceType": "MedicationRequest",
  "id": "med-metformin",
  "status": "active",
  "intent": "order",
  "medication": {
    "concept": {
      "coding": [
        {
          "system": "http://www.nlm.nih.gov/research/umls/rxnorm",
          "code": "860975",
          "display": "Metformin 500mg"
        }
      ],
      "text": "Metformin 500mg Tablet"
    }
  },
  "subject": {
    "reference": "Patient/example-patient-1",
    "display": "John Smith"
  },
  "authoredOn": "2025-10-01",
  "requester": {
    "reference": "Practitioner/dr-johnson",
    "display": "Dr. Sarah Johnson"
  },
  "dosageInstruction": [
    {
      "text": "Take 1 tablet by mouth twice daily with meals",
      "timing": {
        "repeat": {
          "frequency": 2,
          "period": 1,
          "periodUnit": "d"
        }
      },
      "route": {
        "coding": [
          {
            "system": "http://snomed.info/sct",
            "code": "26643006",
            "display": "Oral route"
          }
        ]
      },
      "doseAndRate": [
        {
          "doseQuantity": {
            "value": 500,
            "unit": "mg",
            "system": "http://unitsofmeasure.org",
            "code": "mg"
          }
        }
      ],
      "patientInstruction": "Take with breakfast and dinner to reduce stomach upset"
    }
  ],
  "dispenseRequest": {
    "numberOfRepeatsAllowed": 3,
    "quantity": {
      "value": 60,
      "unit": "tablets",
      "system": "http://unitsofmeasure.org",
      "code": "{tablet}"
    },
    "expectedSupplyDuration": {
      "value": 30,
      "unit": "days",
      "system": "http://unitsofmeasure.org",
      "code": "d"
    },
    "validityPeriod": {
      "start": "2026-01-01"
    }
  }
}
```

### Lisinopril 10mg
```json
{
  "resourceType": "MedicationRequest",
  "id": "med-lisinopril",
  "status": "active",
  "intent": "order",
  "medication": {
    "concept": {
      "coding": [
        {
          "system": "http://www.nlm.nih.gov/research/umls/rxnorm",
          "code": "314076",
          "display": "Lisinopril 10mg"
        }
      ],
      "text": "Lisinopril 10mg Tablet"
    }
  },
  "subject": {
    "reference": "Patient/example-patient-1",
    "display": "John Smith"
  },
  "authoredOn": "2025-09-15",
  "requester": {
    "reference": "Practitioner/dr-johnson",
    "display": "Dr. Sarah Johnson"
  },
  "dosageInstruction": [
    {
      "text": "Take 1 tablet by mouth once daily",
      "timing": {
        "repeat": {
          "frequency": 1,
          "period": 1,
          "periodUnit": "d"
        }
      },
      "route": {
        "coding": [
          {
            "system": "http://snomed.info/sct",
            "code": "26643006",
            "display": "Oral route"
          }
        ]
      },
      "doseAndRate": [
        {
          "doseQuantity": {
            "value": 10,
            "unit": "mg",
            "system": "http://unitsofmeasure.org",
            "code": "mg"
          }
        }
      ],
      "patientInstruction": "Take in the morning. Monitor your blood pressure regularly."
    }
  ],
  "dispenseRequest": {
    "numberOfRepeatsAllowed": 5,
    "quantity": {
      "value": 90,
      "unit": "tablets"
    },
    "expectedSupplyDuration": {
      "value": 90,
      "unit": "days"
    },
    "validityPeriod": {
      "start": "2025-12-20"
    }
  }
}
```

### Atorvastatin 20mg
```json
{
  "resourceType": "MedicationRequest",
  "id": "med-atorvastatin",
  "status": "active",
  "intent": "order",
  "medication": {
    "concept": {
      "coding": [
        {
          "system": "http://www.nlm.nih.gov/research/umls/rxnorm",
          "code": "617318",
          "display": "Atorvastatin 20mg"
        }
      ],
      "text": "Atorvastatin 20mg Tablet"
    }
  },
  "subject": {
    "reference": "Patient/example-patient-1"
  },
  "authoredOn": "2025-08-10",
  "requester": {
    "reference": "Practitioner/dr-chen",
    "display": "Dr. Michael Chen"
  },
  "dosageInstruction": [
    {
      "text": "Take 1 tablet by mouth once daily at bedtime",
      "timing": {
        "repeat": {
          "frequency": 1,
          "period": 1,
          "periodUnit": "d",
          "when": ["HS"]
        }
      },
      "patientInstruction": "Take at bedtime for best cholesterol control. Avoid grapefruit juice."
    }
  ],
  "dispenseRequest": {
    "numberOfRepeatsAllowed": 2,
    "validityPeriod": {
      "start": "2025-12-15"
    }
  }
}
```

---

## 4. Diagnostic Report & Observations

### Complete Blood Count (CBC) - With Abnormal Values

#### DiagnosticReport
```json
{
  "resourceType": "DiagnosticReport",
  "id": "report-cbc-jan2026",
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/v2-0074",
          "code": "LAB",
          "display": "Laboratory"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "58410-2",
        "display": "Complete blood count (CBC) panel - Blood by Automated count"
      }
    ],
    "text": "Complete Blood Count"
  },
  "subject": {
    "reference": "Patient/example-patient-1",
    "display": "John Smith"
  },
  "effectiveDateTime": "2026-01-05T08:30:00Z",
  "issued": "2026-01-05T14:00:00Z",
  "performer": [
    {
      "reference": "Practitioner/dr-johnson",
      "display": "Dr. Sarah Johnson"
    }
  ],
  "result": [
    {
      "reference": "Observation/cbc-wbc"
    },
    {
      "reference": "Observation/cbc-rbc"
    },
    {
      "reference": "Observation/cbc-hemoglobin"
    },
    {
      "reference": "Observation/cbc-hematocrit"
    },
    {
      "reference": "Observation/cbc-platelet"
    }
  ],
  "conclusion": "Mild anemia noted. Patient shows low RBC count, hemoglobin, and hematocrit. Recommend iron supplementation and follow-up in 3 months."
}
```

#### Observation: WBC Count (Normal)
```json
{
  "resourceType": "Observation",
  "id": "cbc-wbc",
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/observation-category",
          "code": "laboratory"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "6690-2",
        "display": "Leukocytes [#/volume] in Blood"
      }
    ],
    "text": "WBC Count"
  },
  "subject": {
    "reference": "Patient/example-patient-1"
  },
  "effectiveDateTime": "2026-01-05T08:30:00Z",
  "valueQuantity": {
    "value": 8.5,
    "unit": "10*3/uL",
    "system": "http://unitsofmeasure.org",
    "code": "10*3/uL"
  },
  "interpretation": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
          "code": "N",
          "display": "Normal"
        }
      ]
    }
  ],
  "referenceRange": [
    {
      "low": {
        "value": 4.5,
        "unit": "10*3/uL"
      },
      "high": {
        "value": 11.0,
        "unit": "10*3/uL"
      }
    }
  ]
}
```

#### Observation: RBC Count (Low)
```json
{
  "resourceType": "Observation",
  "id": "cbc-rbc",
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/observation-category",
          "code": "laboratory"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "789-8",
        "display": "Erythrocytes [#/volume] in Blood"
      }
    ],
    "text": "RBC Count"
  },
  "subject": {
    "reference": "Patient/example-patient-1"
  },
  "effectiveDateTime": "2026-01-05T08:30:00Z",
  "valueQuantity": {
    "value": 4.2,
    "unit": "10*6/uL",
    "system": "http://unitsofmeasure.org",
    "code": "10*6/uL"
  },
  "interpretation": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
          "code": "L",
          "display": "Low"
        }
      ]
    }
  ],
  "referenceRange": [
    {
      "low": {
        "value": 4.5,
        "unit": "10*6/uL"
      },
      "high": {
        "value": 5.5,
        "unit": "10*6/uL"
      }
    }
  ]
}
```

#### Observation: Hemoglobin (Low)
```json
{
  "resourceType": "Observation",
  "id": "cbc-hemoglobin",
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/observation-category",
          "code": "laboratory"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "718-7",
        "display": "Hemoglobin [Mass/volume] in Blood"
      }
    ],
    "text": "Hemoglobin"
  },
  "subject": {
    "reference": "Patient/example-patient-1"
  },
  "effectiveDateTime": "2026-01-05T08:30:00Z",
  "valueQuantity": {
    "value": 13.1,
    "unit": "g/dL",
    "system": "http://unitsofmeasure.org",
    "code": "g/dL"
  },
  "interpretation": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
          "code": "L",
          "display": "Low"
        }
      ]
    }
  ],
  "referenceRange": [
    {
      "low": {
        "value": 14.0,
        "unit": "g/dL"
      },
      "high": {
        "value": 18.0,
        "unit": "g/dL"
      }
    }
  ]
}
```

#### Observation: Hematocrit (Low)
```json
{
  "resourceType": "Observation",
  "id": "cbc-hematocrit",
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/observation-category",
          "code": "laboratory"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "4544-3",
        "display": "Hematocrit [Volume Fraction] of Blood"
      }
    ],
    "text": "Hematocrit"
  },
  "subject": {
    "reference": "Patient/example-patient-1"
  },
  "effectiveDateTime": "2026-01-05T08:30:00Z",
  "valueQuantity": {
    "value": 39,
    "unit": "%",
    "system": "http://unitsofmeasure.org",
    "code": "%"
  },
  "interpretation": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
          "code": "L",
          "display": "Low"
        }
      ]
    }
  ],
  "referenceRange": [
    {
      "low": {
        "value": 42,
        "unit": "%"
      },
      "high": {
        "value": 52,
        "unit": "%"
      }
    }
  ]
}
```

#### Observation: Platelet Count (Normal)
```json
{
  "resourceType": "Observation",
  "id": "cbc-platelet",
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/observation-category",
          "code": "laboratory"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "777-3",
        "display": "Platelets [#/volume] in Blood"
      }
    ],
    "text": "Platelet Count"
  },
  "subject": {
    "reference": "Patient/example-patient-1"
  },
  "effectiveDateTime": "2026-01-05T08:30:00Z",
  "valueQuantity": {
    "value": 250,
    "unit": "10*3/uL",
    "system": "http://unitsofmeasure.org",
    "code": "10*3/uL"
  },
  "interpretation": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
          "code": "N",
          "display": "Normal"
        }
      ]
    }
  ],
  "referenceRange": [
    {
      "low": {
        "value": 150,
        "unit": "10*3/uL"
      },
      "high": {
        "value": 400,
        "unit": "10*3/uL"
      }
    }
  ]
}
```

### Lipid Panel - Normal Results

#### DiagnosticReport
```json
{
  "resourceType": "DiagnosticReport",
  "id": "report-lipid-dec2025",
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/v2-0074",
          "code": "LAB"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "57698-3",
        "display": "Lipid panel with direct LDL - Serum or Plasma"
      }
    ],
    "text": "Lipid Panel"
  },
  "subject": {
    "reference": "Patient/example-patient-1"
  },
  "effectiveDateTime": "2025-12-28T07:45:00Z",
  "issued": "2025-12-28T12:30:00Z",
  "performer": [
    {
      "reference": "Practitioner/dr-chen",
      "display": "Dr. Michael Chen"
    }
  ],
  "result": [
    {
      "reference": "Observation/lipid-total-cholesterol"
    },
    {
      "reference": "Observation/lipid-ldl"
    },
    {
      "reference": "Observation/lipid-hdl"
    },
    {
      "reference": "Observation/lipid-triglycerides"
    }
  ],
  "conclusion": "Lipid levels within optimal range. Continue current statin therapy."
}
```

#### Observation: Total Cholesterol
```json
{
  "resourceType": "Observation",
  "id": "lipid-total-cholesterol",
  "status": "final",
  "category": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/observation-category",
          "code": "laboratory"
        }
      ]
    }
  ],
  "code": {
    "coding": [
      {
        "system": "http://loinc.org",
        "code": "2093-3",
        "display": "Cholesterol [Mass/volume] in Serum or Plasma"
      }
    ],
    "text": "Total Cholesterol"
  },
  "subject": {
    "reference": "Patient/example-patient-1"
  },
  "effectiveDateTime": "2025-12-28T07:45:00Z",
  "valueQuantity": {
    "value": 185,
    "unit": "mg/dL",
    "system": "http://unitsofmeasure.org",
    "code": "mg/dL"
  },
  "interpretation": [
    {
      "coding": [
        {
          "system": "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
          "code": "N",
          "display": "Normal"
        }
      ]
    }
  ],
  "referenceRange": [
    {
      "high": {
        "value": 200,
        "unit": "mg/dL"
      },
      "text": "Desirable: <200 mg/dL"
    }
  ]
}
```

---

## 5. Coverage Resource (for Member Portal benefits)

```json
{
  "resourceType": "Coverage",
  "id": "coverage-sub001",
  "status": "active",
  "type": {
    "coding": [
      {
        "system": "http://terminology.hl7.org/CodeSystem/v3-ActCode",
        "code": "HIP",
        "display": "health insurance plan policy"
      }
    ]
  },
  "subscriber": {
    "reference": "Patient/example-patient-1"
  },
  "beneficiary": {
    "reference": "Patient/example-patient-1"
  },
  "period": {
    "start": "2026-01-01",
    "end": "2026-12-31"
  },
  "payor": [
    {
      "display": "ClaimsIQ Healthcare"
    }
  ],
  "class": [
    {
      "type": {
        "coding": [
          {
            "system": "http://terminology.hl7.org/CodeSystem/coverage-class",
            "code": "plan"
          }
        ]
      },
      "value": "Gold PPO",
      "name": "Gold PPO Plan"
    }
  ],
  "costToBeneficiary": [
    {
      "type": {
        "coding": [
          {
            "code": "deductible"
          }
        ]
      },
      "valueMoney": {
        "value": 2000,
        "currency": "USD"
      }
    },
    {
      "type": {
        "coding": [
          {
            "code": "maxoutofpocket"
          }
        ]
      },
      "valueMoney": {
        "value": 5000,
        "currency": "USD"
      }
    }
  ]
}
```

---

## PowerShell Script to Batch Upload

Save as `upload-fhir-data.ps1`:

```powershell
# Configuration
$FhirUrl = "https://ahdswstest001-fhirr4test001.fhir.azurehealthcareapis.com"
$Token = (az account get-access-token --resource="$FhirUrl" --query accessToken -o tsv)

# Upload function
function Upload-FhirResource {
    param (
        [string]$ResourceType,
        [string]$JsonFile
    )
    
    $headers = @{
        "Authorization" = "Bearer $Token"
        "Content-Type" = "application/fhir+json"
    }
    
    $body = Get-Content $JsonFile -Raw
    
    try {
        $response = Invoke-RestMethod -Uri "$FhirUrl/$ResourceType" -Method Post -Headers $headers -Body $body
        Write-Host "✅ Uploaded $ResourceType/$($response.id)" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to upload $JsonFile : $_" -ForegroundColor Red
    }
}

# Upload all resources
Upload-FhirResource -ResourceType "Patient" -JsonFile "patient-example.json"
Upload-FhirResource -ResourceType "Practitioner" -JsonFile "practitioner-johnson.json"
Upload-FhirResource -ResourceType "Practitioner" -JsonFile "practitioner-chen.json"
Upload-FhirResource -ResourceType "MedicationRequest" -JsonFile "med-metformin.json"
Upload-FhirResource -ResourceType "MedicationRequest" -JsonFile "med-lisinopril.json"
Upload-FhirResource -ResourceType "MedicationRequest" -JsonFile "med-atorvastatin.json"
Upload-FhirResource -ResourceType "DiagnosticReport" -JsonFile "report-cbc.json"
Upload-FhirResource -ResourceType "Observation" -JsonFile "obs-cbc-wbc.json"
Upload-FhirResource -ResourceType "Observation" -JsonFile "obs-cbc-rbc.json"
Upload-FhirResource -ResourceType "Observation" -JsonFile "obs-cbc-hemoglobin.json"
Upload-FhirResource -ResourceType "Observation" -JsonFile "obs-cbc-hematocrit.json"
Upload-FhirResource -ResourceType "Observation" -JsonFile "obs-cbc-platelet.json"
Upload-FhirResource -ResourceType "DiagnosticReport" -JsonFile "report-lipid.json"
Upload-FhirResource -ResourceType "Observation" -JsonFile "obs-lipid-total.json"
Upload-FhirResource -ResourceType "Coverage" -JsonFile "coverage-sub001.json"

Write-Host "`n✅ Upload complete!" -ForegroundColor Green
```

---

## Testing the Data

After uploading, test with these FHIR searches:

```bash
# Get patient
GET {{fhirUrl}}/Patient/example-patient-1

# Get active medications
GET {{fhirUrl}}/MedicationRequest?patient=Patient/example-patient-1&status=active

# Get test results
GET {{fhirUrl}}/DiagnosticReport?patient=Patient/example-patient-1&_sort=-date

# Get specific observation
GET {{fhirUrl}}/Observation/cbc-wbc
```

---

**Ready to Test!** Load this data into your Azure FHIR server and the Medications and Test Results screens will display real data.
