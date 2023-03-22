export interface DeviceOverview {
    deviceTypeId: number;
    deviceTypeName: string;
    totalAmount: number;
    inStockAmount: number;
    inUseAmount: number;
    otherAmount: number;
    inStockPercentage: number;
    inUsePercentage: number;
}