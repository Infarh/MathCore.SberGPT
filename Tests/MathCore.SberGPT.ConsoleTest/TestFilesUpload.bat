@echo Hello World! > test.txt

curl -L -X POST "http://localhost:8080/api/v1/files" ^
-H "Content-Type: multipart/form-data" ^
-H "Accept: application/json" ^
-H "Authorization: Bearer test_token" ^
-F "file=@test.txt" ^
-F purpose="general"

@ del /Q test.txt > nul