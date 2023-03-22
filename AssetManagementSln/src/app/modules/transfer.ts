export interface Transfer {
    transferDate: Date,
    deviceTypeName?: string,
    deviceServiceTag?: string,
    transferFrom: string,
    transferTo: string,
    amount?: number,
    transferTypeName: string
}