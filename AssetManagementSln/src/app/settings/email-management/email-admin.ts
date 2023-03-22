export interface EmailAdmin {
    emailAdmin: string,
    smtpHost?: string,
    smtpPort: number,
    enableSsl: boolean,
    emailFrom: string,
    titleFrom: string

}