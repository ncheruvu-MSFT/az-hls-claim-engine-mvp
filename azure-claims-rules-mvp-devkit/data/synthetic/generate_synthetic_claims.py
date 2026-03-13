
import csv, random, datetime

cpts = ["99213","99214","99205","93000","J1234","J9876","A5500","A5513","V2020"]
icds = ["E11.9","I10","M54.5","H52.4","J45.909","K21.9","Z00.00","R53.83"]

rows = []
for i in range(50):
    rows.append({
        "ClaimId": f"C{i+1}",
        "MemberId": f"M{random.randint(1,20)}",
        "ProviderId": f"P{random.randint(1,10)}",
        "PlanId": f"Plan{random.randint(1,5)}",
        "ServiceDate": (datetime.date(2025,11,1) + datetime.timedelta(days=random.randint(0,60))).isoformat(),
        "DiagnosisCode": random.choice(icds),
        "ProcedureCode": random.choice(cpts),
        "Units": str(random.randint(1,3)),
        "ChargeAmount": f"{random.randint(50,500)}.00",
    })

with open('claims_50.csv','w',newline='') as f:
    w = csv.DictWriter(f, fieldnames=["ClaimId","MemberId","ProviderId","PlanId","ServiceDate","DiagnosisCode","ProcedureCode","Units","ChargeAmount"])
    w.writeheader(); w.writerows(rows)
print('Wrote claims_50.csv')
