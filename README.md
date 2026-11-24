การพัฒนาสื่อการสอนวิชาดนตรีไทยโดยใช้ระบบจำลองเสมือน (ระนาดเอก)

Development of Thai Music Instructional Media Using a Virtual Reality System (Ranad Ek)

โครงงานวิชา ENGSE211 สาขาวิศวกรรมซอฟต์แวร์ มหาวิทยาลัยเทคโนโลยีราชมงคลล้านนา ปีการศึกษา 1/2569

แอป VR สำหรับฝึกตีระนาดเอก บนแว่น Meta Quest 2 ให้ผู้เริ่มเรียนซ้อมได้แม้ไม่มีระนาดจริงอยู่ที่บ้าน

ภาพรวม

ผู้เริ่มเรียนระนาดเอกซ้อมได้เฉพาะตอนที่มีเครื่องและมีครู จึงฝึกซ้ำได้ไม่พอ โครงงานนี้สร้างระนาดเอกเสมือนใน VR ให้ผู้เรียนใช้มือหรือไม้ตีฝึกตามโน้ตไทย โดยมีไฟนำทางและระบบประเมินผลการตี

ฟีเจอร์
ระนาดเอกเสมือน 21 ลูก ตีได้ด้วยมือในแว่น VR
โน้ตไทย (ด ร ม ฟ ซ ล ท) แสดงตำแหน่งลูกที่ต้องตี และไฮไลต์สีตามมือซ้าย/ขวา
โหมดบทเรียน: ตัวอย่าง, ฝึก, เล่น, ทดสอบ
ระบบคะแนนจากความแม่นยำของลูกระนาดและมือที่ใช้ รวมกับความตรงจังหวะ
โหมดทดสอบไม่มีไฟนำทางและเก็บคะแนนสูงสุดของแต่ละบทเรียน

สถานะ: อยู่ระหว่างพัฒนา ฟีเจอร์บางส่วนอาจยังไม่ครบ

เทคโนโลยีที่ใช้
ส่วน	รายละเอียด
Engine	Unity 6 (Universal Render Pipeline)
XR	XR Interaction Toolkit, XR Hands, OpenXR
อุปกรณ์	Meta Quest 2 (ใช้ Quest Link ระหว่างพัฒนาได้)
วิธีเปิดโปรเจกต์
ติดตั้ง Unity Hub และ Unity 6 พร้อมโมดูล Android Build Support (ดูเวอร์ชันที่ตรงในไฟล์ ProjectSettings/ProjectVersion.txt)
โคลน repo นี้
git clone https://github.com/Veerawat2005/Development-of-Thai-Music-Instructional-Media-Using-a-Virtual-Reality-System-Ranad-Ek-.git

ถ้ามีไฟล์ขนาดใหญ่ ให้ติดตั้ง Git LFS ก่อน (git lfs install) 3. เปิด Unity Hub → Add → เลือกโฟลเดอร์ที่โคลนมา 4. เปิดครั้งแรกจะช้า เพราะ Unity สร้างโฟลเดอร์ Library ใหม่ 5. เปิดฉากหลักใน Assets/Scenes/

วิธีรันบน Meta Quest 2
ระหว่างพัฒนา: ต่อ Quest 2 ด้วยสาย USB หรือ Air Link เปิด Quest Link แล้วกด Play ใน Unity
ติดตั้งลงแว่น: File → Build Settings → Android → Build And Run (เปิดโหมดนักพัฒนาในแอป Meta Horizon ก่อน)
โครงสร้างโฟลเดอร์
Assets/          โค้ด ฉาก โมเดล เสียง และบทเรียน
Packages/        รายการแพ็กเกจของ Unity
ProjectSettings/ การตั้งค่าโปรเจกต์

โฟลเดอร์ Library, Temp, Logs และ UserSettings ไม่ได้อยู่ใน repo (สร้างใหม่ได้เอง)

ผู้จัดทำ
ชื่อ	บทบาท
วีราวัชร์ สุประดิษฐ์พงศ์	หัวหน้าโครงงาน
ชยานันท์ เพชรรักษ์	ผู้ร่วมโครงงาน

อาจารย์ที่ปรึกษา: นุรักษ์ ไชยศรี

English summary

Development of Thai Music Instructional Media Using a Virtual Reality System (Ranad Ek)

A VR application for practicing the Thai xylophone ranad ek on Meta Quest 2. It lets beginners practice without a physical instrument. Learners strike virtual bars by hand while following Thai notation with colour-coded left/right hand guidance, and receive scores for key and hand accuracy and timing.

Tech stack: Unity 6 (URP), XR Interaction Toolkit, XR Hands, OpenXR, Meta Quest 2.

Getting started: install Unity Hub and the Unity 6 version listed in ProjectSettings/ProjectVersion.txt (with Android Build Support), clone this repository, open the folder from Unity Hub, then open the main scene in Assets/Scenes/.

Team: Veerawat Supraditpong (lead), Chayanan Phetrak. Advisor: Nurak Chaiyasri.

Status: work in progress. Course project, ENGSE211, Rajamangala University of Technology Lanna, academic year 1/2569.
