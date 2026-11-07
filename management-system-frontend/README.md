# Task Management System — Angular Frontend

واجهة عربية متجاوبة مبنية بـAngular Standalone Components وTypeScript وReactive Forms. تتصل بالـASP.NET Core API الفعلي عبر proxy التطوير في `proxy.conf.json`.

## المتطلبات والتشغيل

- Node.js 20.19+ أو 22.12+.
- تشغيل الـAPI بملف `https` على `https://localhost:7240` مع PostgreSQL جاهز.

من هذا المجلد:

```powershell
npm install
npm start -- --host 127.0.0.1 --port 4200
```

افتح `http://127.0.0.1:4200` وشغّل نسخة واحدة فقط من خادم Angular. طلبات `/api` تُحوّل أثناء التطوير إلى `https://localhost:7240/api` عبر شهادة التطوير المحلية، لذلك شغّل الـAPI بملف `https` قبل تسجيل الدخول.

## المسارات والميزات الحالية

- `/login` — تسجيل الدخول باستخدام JWT.
- `/register` — إنشاء حساب جديد؛ بعد نجاح التسجيل ينتقل المستخدم مباشرة إلى لوحة التحكم.
- `/dashboard` — صفحة محمية تعرض إحصائيات المهام والمهام الأخيرة والأولوية العالية من الـAPI.
- `/tasks` — صفحة محمية لعرض المهام والبحث والتصفية والإنشاء والتعديل والحذف وإسناد المهمة وعرض سجل نشاطها.

تُحفظ جلسة تسجيل الدخول في `sessionStorage`، ويُرسل interceptor رمز JWT مع طلبات `/api`. تنتهي الجلسة عند تسجيل الخروج أو إغلاق جلسة المتصفح.

## البنية

- `src/app/core/auth` — أنواع المصادقة، تخزين الجلسة، خدمة الاتصال، interceptor والحارس.
- `src/app/core/dashboard` — أنواع بيانات لوحة التحكم وخدمة الـAPI.
- `src/app/features/auth` — صفحتا تسجيل الدخول وإنشاء الحساب.
- `src/app/features/dashboard` — لوحة التحكم المتصلة بالـAPI.
- `src/app/app.routes.ts` — مسارات التطبيق.
- `src/app/app.config.ts` — إعداد Router وHttpClient وAPI base URL.

## ملاحظات التشغيل

شغّل الواجهة على `http://127.0.0.1:4200` وتجنب تشغيل نسختين منها على المنفذ نفسه. ملف `proxy.conf.json` يمرر `/api` إلى `https://localhost:7240` عبر شهادة التطوير المحلية؛ لذلك يجب تشغيل ASP.NET Core بملف `https` كما هو موضح في README الرئيسي.
