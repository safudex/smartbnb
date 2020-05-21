import nodemailer from 'nodemailer';

export default class Emailer {
    private transporter:any;

    constructor(private toEmailAddress:string) {
      // Use ethereal.email
      nodemailer.createTestAccount().then((account) => {
        this.transporter = nodemailer.createTransport({
          host: 'smtp.ethereal.email',
          port: 587,
          secure: false, // true for 465, false for other ports
          auth: {
            user: account.user, // generated ethereal user
            pass: account.pass, // generated ethereal password
          },
        });
      });
    }

    public async send(message:string) {
      await this.transporter.sendMail({
        from: '"Collateral provider" <collateral@smartbnb.net>',
        to: this.toEmailAddress,
        subject: 'Updates on your collateral provider',
        text: message,
      });
    }
}
