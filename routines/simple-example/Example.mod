MODULE Program4;

VAR
  C:CHAR;

  PROCEDURE A;
  BEGIN
     WRITE(C);
     C := 'a'
  END A;

BEGIN
  C := 'm';
  WRITE(C);
  A;
  WRITE(C)
END Program4.