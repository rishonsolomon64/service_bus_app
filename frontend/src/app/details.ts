export interface NestedDetails {
  id: string;
  customerId: string;
  videoIntegrationType: string;
}

export interface Details {
  type: string;
  vmsIntegration: NestedDetails;
  sequenceNumber: string;
  deviceIds: string;
}

