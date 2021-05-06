<?php
    $txtcontent = $_REQUEST['txt'];
    $fp = fopen('animTime.txt', w);
    fwrite($fp, $txtcontent);
    fclose($fp);
?>

