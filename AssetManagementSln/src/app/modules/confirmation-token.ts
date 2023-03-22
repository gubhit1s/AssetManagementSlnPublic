export interface ConfirmationToken {
    token: string,
    userConfirmed: boolean,
    declinedReason?: string
}