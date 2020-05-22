import nodemailer from 'nodemailer';

export default class Emailer {
    private transporter: Promise<nodemailer.Transporter>;

    constructor(private toEmailAddress: string, private fromAddress:string, private subject:string) {
      // Use ethereal.email
      this.transporter = new Promise((resolve, reject) => {
        nodemailer.createTestAccount().then((account) => {
          const transp = nodemailer.createTransport({
            host: 'smtp.ethereal.email',
            port: 587,
            secure: false, // true for 465, false for other ports
            auth: {
              user: account.user, // generated ethereal user
              pass: account.pass, // generated ethereal password
            },
          });
          return resolve(transp);
        }).catch(reject);
      });
    }

    public async send(message: string) {
      await (await this.transporter).sendMail({
        from: this.fromAddress,
        to: this.toEmailAddress,
        subject: this.subject,
        text: message,
      });
    }
}
